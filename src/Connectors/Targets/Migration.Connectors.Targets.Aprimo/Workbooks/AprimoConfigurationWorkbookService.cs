using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Migration.Connectors.Targets.Aprimo.Workbooks;

public sealed class AprimoConfigurationWorkbookService : IAprimoConfigurationWorkbookService
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private readonly HttpClient _httpClient;
    private readonly ILogger<AprimoConfigurationWorkbookService> _logger;

    public AprimoConfigurationWorkbookService(HttpClient httpClient, ILogger<AprimoConfigurationWorkbookService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AprimoConfigurationWorkbookResult> GenerateAsync(
        AprimoConfigurationWorkbookRequest request,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(outputStream);

        var credentials = request.Credentials;
        if (string.IsNullOrWhiteSpace(credentials.SubDomain) || string.IsNullOrWhiteSpace(credentials.ClientId) || string.IsNullOrWhiteSpace(credentials.ClientSecret))
        {
            throw new InvalidOperationException("Aprimo workbook generation requires SubDomain, ClientId, and ClientSecret.");
        }

        var options = request.ExportOptions ?? AprimoConfigurationWorkbookExportOptions.Defaults;
        var token = await GetAccessTokenAsync(credentials, cancellationToken).ConfigureAwait(false);
        var client = new AprimoWorkbookApiClient(_httpClient, credentials.SubDomain.Trim(), token);
        var builder = new AprimoWorkbookDataBuilder(client, _logger, cancellationToken);
        var sheets = await builder.BuildSheetsAsync(options).ConfigureAwait(false);

        XlsxWorkbookWriter.Write(outputStream, sheets);

        var rowCounts = sheets.ToDictionary(x => x.Name, x => Math.Max(0, x.Rows.Count - 1), StringComparer.OrdinalIgnoreCase);
        return new AprimoConfigurationWorkbookResult(
            $"{credentials.SubDomain.Trim().ToUpperInvariant()} ConfigWorkbook {DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx",
            ContentType,
            sheets.Count,
            rowCounts);
    }

    private async Task<string> GetAccessTokenAsync(AprimoConfigurationWorkbookCredentials credentials, CancellationToken cancellationToken)
    {
        var tokenEndpoint = $"https://{credentials.SubDomain.Trim()}.aprimo.com/login/connect/token";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_credentials"] = "configmover",
            ["client_id"] = credentials.ClientId.Trim(),
            ["client_secret"] = credentials.ClientSecret
        });

        using var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Aprimo token request failed: {(int)response.StatusCode} {response.ReasonPhrase}. {json}");
        }

        var root = JsonNode.Parse(json)?.AsObject() ?? throw new JsonException("Aprimo token response was empty.");
        var token = Text(root, "access_token", "accessToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new JsonException("Aprimo token response did not contain access_token.");
        }

        return token;
    }

    internal static string Text(JsonObject obj, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (obj[key] is JsonValue value)
            {
                var text = value.ToString();
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
        }
        return string.Empty;
    }

    internal static string Serialize(JsonNode? node) => node is null ? string.Empty : node.ToJsonString(JsonOptions);
}

internal sealed class AprimoWorkbookApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _damBaseUrl;
    private readonly string _token;

    public AprimoWorkbookApiClient(HttpClient httpClient, string subDomain, string token)
    {
        _httpClient = httpClient;
        _damBaseUrl = $"https://{subDomain}.dam.aprimo.com/api/core";
        _token = token;
    }

    public async Task<IReadOnlyList<JsonObject>> GetAllAsync(string relativeUrl, IDictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        var results = new List<JsonObject>();
        var url = CombineUrl(_damBaseUrl, relativeUrl);

        while (!string.IsNullOrWhiteSpace(url))
        {
            var root = await GetObjectAsync(url, headers, cancellationToken).ConfigureAwait(false);
            if (root["items"] is JsonArray items)
            {
                results.AddRange(items.OfType<JsonObject>());
            }
            else if (root["data"] is JsonArray data)
            {
                results.AddRange(data.OfType<JsonObject>());
            }
            else if (root.Count > 0)
            {
                results.Add(root);
            }

            url = root["_links"]?["next"]?["href"]?.GetValue<string>() ?? string.Empty;
        }

        return results;
    }

    public Task<JsonObject> GetObjectAsync(string relativeOrAbsoluteUrl, IDictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        var url = relativeOrAbsoluteUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? relativeOrAbsoluteUrl
            : CombineUrl(_damBaseUrl, relativeOrAbsoluteUrl);
        return SendObjectAsync(url, headers, cancellationToken);
    }

    private async Task<JsonObject> SendObjectAsync(string url, IDictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/hal+json"));
        request.Headers.TryAddWithoutValidation("API-VERSION", "1");
        request.Headers.TryAddWithoutValidation("pageSize", "500");
        request.Headers.TryAddWithoutValidation("Languages", "*");
        if (headers is not null)
        {
            foreach (var header in headers)
            {
                request.Headers.Remove(header.Key);
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Aprimo GET failed: {(int)response.StatusCode} {response.ReasonPhrase}. Url='{url}'. {json}");
        }

        return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
    }

    private static string CombineUrl(string baseUrl, string path) => baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
}

internal sealed class AprimoWorkbookDataBuilder
{
    private readonly AprimoWorkbookApiClient _client;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;
    private Dictionary<string, JsonObject> _classifications = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, JsonObject> _fieldGroups = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, JsonObject> _fieldDefinitions = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, JsonObject> _userGroups = new(StringComparer.OrdinalIgnoreCase);

    public AprimoWorkbookDataBuilder(AprimoWorkbookApiClient client, ILogger logger, CancellationToken cancellationToken)
    {
        _client = client;
        _logger = logger;
        _cancellationToken = cancellationToken;
    }

    public async Task<List<XlsxWorksheet>> BuildSheetsAsync(AprimoConfigurationWorkbookExportOptions options)
    {
        await PreloadAsync(options).ConfigureAwait(false);
        var sheets = new List<XlsxWorksheet>
        {
            CoverSheet(options)
        };

        if (options.UserGroups) sheets.Add(await UserGroupsSheetAsync().ConfigureAwait(false));
        if (options.FieldGroups) sheets.Add(await FieldGroupsSheetAsync().ConfigureAwait(false));
        if (options.FieldDefinitions) sheets.Add(await FieldDefinitionsSheetAsync(options).ConfigureAwait(false));
        if (options.Classifications) sheets.Add(await ClassificationsSheetAsync(options).ConfigureAwait(false));
        if (options.ClassificationPermissions) sheets.Add(await ClassificationPermissionsSheetAsync().ConfigureAwait(false));
        if (options.FunctionalPermissions) sheets.Add(await FunctionalPermissionsSheetAsync().ConfigureAwait(false));
        if (options.Settings) sheets.Add(await GenericItemsSheetAsync("Settings", "/settings/definitions", includeDetail: true).ConfigureAwait(false));
        if (options.Watermarks) sheets.Add(await GenericItemsSheetAsync("Watermarks", "/watermarks", includeDetail: false).ConfigureAwait(false));
        if (options.Translations) sheets.Add(await GenericItemsSheetAsync("Translations", "/translations", includeDetail: false).ConfigureAwait(false));
        if (options.Rules) sheets.Add(await RulesSheetAsync().ConfigureAwait(false));
        if (options.ContentTypes) sheets.Add(await GenericItemsSheetAsync("Content types", "/contenttypes", includeDetail: false).ConfigureAwait(false));

        return sheets;
    }

    private async Task PreloadAsync(AprimoConfigurationWorkbookExportOptions options)
    {
        if (options.Classifications || options.ClassificationPermissions || options.FieldDefinitions || options.Rules)
        {
            _classifications = await LoadDictionaryAsync("/classifications", new Dictionary<string, string> { ["select-classification"] = "NamePath" }).ConfigureAwait(false);
        }
        if (options.FieldGroups || options.FieldDefinitions || options.Classifications)
        {
            _fieldGroups = await LoadDictionaryAsync("/fieldgroups", null).ConfigureAwait(false);
        }
        if (options.FieldDefinitions || options.Classifications || options.Rules)
        {
            _fieldDefinitions = await LoadDictionaryAsync("/fielddefinitions", null).ConfigureAwait(false);
        }
        if (options.UserGroups || options.FunctionalPermissions || options.ClassificationPermissions)
        {
            _userGroups = await LoadDictionaryAsync("/usergroups", null).ConfigureAwait(false);
        }
    }

    private async Task<Dictionary<string, JsonObject>> LoadDictionaryAsync(string url, IDictionary<string, string>? headers)
    {
        var items = await _client.GetAllAsync(url, headers, _cancellationToken).ConfigureAwait(false);
        return items.Where(x => !string.IsNullOrWhiteSpace(GetText(x, "id")))
            .ToDictionary(x => GetText(x, "id"), x => x, StringComparer.OrdinalIgnoreCase);
    }

    private static XlsxWorksheet CoverSheet(AprimoConfigurationWorkbookExportOptions options)
    {
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "Configuration Workbook" },
            new[] { "Generated UTC", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) },
            new[] { "Export Section", "Selected" }
        };
        rows.AddRange(options.ToLegacySelectionMap().Select(x => new[] { x.Key, x.Value.ToString(CultureInfo.InvariantCulture) }));
        return new XlsxWorksheet("Cover Sheet", rows);
    }

    private Task<XlsxWorksheet> UserGroupsSheetAsync()
    {
        var rows = Start("Id", "Name", "Description", "Status", "RawJson");
        foreach (var item in _userGroups.Values.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new[] { GetText(item, "id"), GetText(item, "name"), GetText(item, "description"), GetText(item, "status"), Raw(item) });
        }
        return Task.FromResult(new XlsxWorksheet("User groups", rows));
    }

    private Task<XlsxWorksheet> FieldGroupsSheetAsync()
    {
        var rows = Start("Id", "Name", "Label", "Description", "RawJson");
        foreach (var item in _fieldGroups.Values.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new[] { GetText(item, "id"), GetText(item, "name"), Label(item), GetText(item, "description"), Raw(item) });
        }
        return Task.FromResult(new XlsxWorksheet("Field groups", rows));
    }

    private Task<XlsxWorksheet> FieldDefinitionsSheetAsync(AprimoConfigurationWorkbookExportOptions options)
    {
        var headers = new List<string> { "Id", "Name", "Label", "DataType", "Required", "MultiValue", "FieldGroup", "RawJson" };
        if (options.Languages) headers.InsertRange(3, LanguageHeaders());
        var rows = new List<IReadOnlyList<string>> { headers };
        foreach (var item in _fieldDefinitions.Values.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            var row = new List<string> { GetText(item, "id"), GetText(item, "name"), Label(item) };
            if (options.Languages) row.AddRange(LanguageLabels(item));
            row.AddRange(new[] { GetText(item, "dataType", "type", "fieldType"), BoolText(item, "required", "isRequired"), BoolText(item, "multiValue", "isMultiValue"), JoinNames(item["fieldGroups"], _fieldGroups), Raw(item) });
            rows.Add(row);
        }
        return Task.FromResult(new XlsxWorksheet("Field definitions", rows));
    }

    private Task<XlsxWorksheet> ClassificationsSheetAsync(AprimoConfigurationWorkbookExportOptions options)
    {
        var headers = new List<string> { "Id", "Name", "Label", "Identifier", "NamePath", "Registered Field Groups", "Registered Fields", "RawJson" };
        if (options.Languages) headers.InsertRange(3, LanguageHeaders());
        var rows = new List<IReadOnlyList<string>> { headers };
        foreach (var item in _classifications.Values.OrderBy(x => GetText(x, "namePath", "name"), StringComparer.OrdinalIgnoreCase))
        {
            var row = new List<string> { GetText(item, "id"), GetText(item, "name"), Label(item) };
            if (options.Languages) row.AddRange(LanguageLabels(item));
            row.AddRange(new[] { GetText(item, "identifier"), GetText(item, "namePath"), JoinNames(item["registeredFieldGroups"], _fieldGroups), JoinNames(item["registeredFields"], _fieldDefinitions), Raw(item) });
            rows.Add(row);
        }
        return Task.FromResult(new XlsxWorksheet("Classifications", rows));
    }

    private async Task<XlsxWorksheet> ClassificationPermissionsSheetAsync()
    {
        var rows = Start("ClassificationId", "ClassificationPath", "PermissionType", "RawJson");
        foreach (var classification in _classifications.Values.OrderBy(x => GetText(x, "namePath", "name"), StringComparer.OrdinalIgnoreCase))
        {
            var id = GetText(classification, "id");
            if (string.IsNullOrWhiteSpace(id)) continue;
            var path = GetText(classification, "namePath", "name");
            foreach (var permissionType in new[] { "ClassificationTreePermissions", "RecordPermissions", "DownloadPermissions" })
            {
                try
                {
                    var item = await _client.GetObjectAsync($"/classification/{id}/{permissionType}", null, _cancellationToken).ConfigureAwait(false);
                    rows.Add(new[] { id, path, permissionType, Raw(item) });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed reading Aprimo classification permission {PermissionType} for {ClassificationId}.", permissionType, id);
                    rows.Add(new[] { id, path, permissionType, $"ERROR: {ex.Message}" });
                }
            }
        }
        return new XlsxWorksheet("Classification Permissions", rows);
    }

    private async Task<XlsxWorksheet> FunctionalPermissionsSheetAsync()
    {
        var permissions = await _client.GetAllAsync("/permissions", null, _cancellationToken).ConfigureAwait(false);
        var rows = Start("Permission", "Label", "RawJson");
        foreach (var item in permissions.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new[] { GetText(item, "name"), Label(item), Raw(item) });
        }
        return new XlsxWorksheet("Functional Permissions", rows);
    }

    private async Task<XlsxWorksheet> RulesSheetAsync()
    {
        var items = await _client.GetAllAsync("/rules", new Dictionary<string, string> { ["select-Rule"] = "conditions, actions" }, _cancellationToken).ConfigureAwait(false);
        var rows = Start("Id", "Name", "Status", "Conditions", "Actions", "RawJson");
        foreach (var item in items.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new[] { GetText(item, "id"), GetText(item, "name"), GetText(item, "status"), Raw(item["conditions"]), Raw(item["actions"]), Raw(item) });
        }
        return new XlsxWorksheet("Rules", rows);
    }

    private async Task<XlsxWorksheet> GenericItemsSheetAsync(string name, string url, bool includeDetail)
    {
        var items = await _client.GetAllAsync(url, null, _cancellationToken).ConfigureAwait(false);
        var rows = Start("Id", "Name", "Label", "Status", "RawJson");
        foreach (var item in items.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new[] { GetText(item, "id", "name"), GetText(item, "name"), Label(item), GetText(item, "status"), Raw(item) });
        }
        return new XlsxWorksheet(name, rows);
    }

    private static List<IReadOnlyList<string>> Start(params string[] headers) => new() { headers };
    private static string Raw(JsonNode? node) => AprimoConfigurationWorkbookService.Serialize(node);
    private static string GetText(JsonObject obj, params string[] keys) => AprimoConfigurationWorkbookService.Text(obj, keys);
    private static string Label(JsonObject obj) => FindLabel(obj["labels"]);
    private static string BoolText(JsonObject obj, params string[] keys) => keys.Select(k => obj[k]).OfType<JsonValue>().Select(v => v.ToString()).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
    private static string[] LanguageHeaders() => new[] { "Label Spanish", "Label French", "Label German", "Label Italian", "Label Japanese", "Label Chinese Simplified", "Label Portuguese" };
    private static string[] LanguageLabels(JsonObject obj) => new[] { "4877d795-541c-4fac-86b9-9a06830b08ee", "18ca1f44-0c21-4eed-aecc-b98f0ae65a81", "691a7c86-57dc-4330-a512-c616db954d6a", "5ec55442-1cac-4734-a0ed-e1d01cd1e49f", "e6665327-1314-43c0-924f-9366e77064d7", "bcda9def-f15e-4ab2-90d1-99f2d0428f5e", "1e498386-288d-41d0-9e5b-beb8580ffd73" }.Select(id => FindLabel(obj["labels"], id)).ToArray();
    private static string FindLabel(JsonNode? labels, string languageId = "c2bd4f9b-bb95-4bcb-80c3-1e924c9c26dc")
    {
        if (labels is not JsonArray arr) return string.Empty;
        foreach (var item in arr.OfType<JsonObject>())
        {
            if (string.Equals(GetText(item, "languageId", "language"), languageId, StringComparison.OrdinalIgnoreCase))
            {
                return GetText(item, "value", "label", "name");
            }
        }
        return arr.OfType<JsonObject>().Select(x => GetText(x, "value", "label", "name")).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
    }
    private static string JoinNames(JsonNode? values, Dictionary<string, JsonObject> lookup)
    {
        if (values is not JsonArray arr) return string.Empty;
        var names = new List<string>();
        foreach (var node in arr)
        {
            var id = node switch { JsonValue v => v.ToString(), JsonObject o => GetText(o, "id"), _ => string.Empty };
            if (lookup.TryGetValue(id, out var item)) names.Add(GetText(item, "name", "namePath", "id"));
            else if (!string.IsNullOrWhiteSpace(id)) names.Add(id);
        }
        return string.Join(", ", names);
    }
}

internal sealed record XlsxWorksheet(string Name, IReadOnlyList<IReadOnlyList<string>> Rows);

internal static class XlsxWorkbookWriter
{
    public static void Write(Stream output, IReadOnlyList<XlsxWorksheet> worksheets)
    {
        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        Add(archive, "[Content_Types].xml", ContentTypes(worksheets));
        Add(archive, "_rels/.rels", RootRels());
        Add(archive, "xl/workbook.xml", Workbook(worksheets));
        Add(archive, "xl/_rels/workbook.xml.rels", WorkbookRels(worksheets));
        Add(archive, "xl/styles.xml", Styles());
        for (var i = 0; i < worksheets.Count; i++)
        {
            Add(archive, $"xl/worksheets/sheet{i + 1}.xml", Worksheet(worksheets[i]));
        }
    }

    private static void Add(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string ContentTypes(IReadOnlyList<XlsxWorksheet> sheets) => $"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>{string.Concat(Enumerable.Range(1, sheets.Count).Select(i => $"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"))}</Types>
""";
    private static string RootRels() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
""";
    private static string Workbook(IReadOnlyList<XlsxWorksheet> sheets) => $"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>{string.Concat(sheets.Select((s, i) => $"<sheet name=\"{Xml(SafeSheetName(s.Name))}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>"))}</sheets></workbook>
""";
    private static string WorkbookRels(IReadOnlyList<XlsxWorksheet> sheets) => $"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">{string.Concat(sheets.Select((_, i) => $"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>"))}<Relationship Id="rId{(sheets.Count + 1).ToString(CultureInfo.InvariantCulture)}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>
""";
    private static string Styles() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF005F7F"/><bgColor indexed="64"/></patternFill></fill></fills><borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="1" borderId="0" xfId="0" applyFont="1" applyFill="1"/></cellXfs></styleSheet>
""";
    private static string Worksheet(XlsxWorksheet sheet)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            sb.Append($"<row r=\"{r + 1}\">");
            var row = sheet.Rows[r];
            for (var c = 0; c < row.Count; c++)
            {
                var style = r == 0 ? " s=\"1\"" : string.Empty;
                sb.Append($"<c r=\"{ColumnName(c + 1)}{r + 1}\" t=\"inlineStr\"{style}><is><t>{Xml(row[c])}</t></is></c>");
            }
            sb.Append("</row>");
        }
        sb.Append("</sheetData><autoFilter ref=\"A1:Z1\"/></worksheet>");
        return sb.ToString();
    }
    private static string ColumnName(int index)
    {
        var name = string.Empty;
        while (index > 0) { var rem = (index - 1) % 26; name = (char)('A' + rem) + name; index = (index - rem - 1) / 26; }
        return name;
    }
    private static string SafeSheetName(string value)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return safe.Length > 31 ? safe[..31] : safe;
    }
    private static string Xml(string? value) => HtmlEncoder.Default.Encode(value ?? string.Empty);
}
