using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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

        var credentials = NormalizeCredentials(request.Credentials);
        if (string.IsNullOrWhiteSpace(credentials.SubDomain) || string.IsNullOrWhiteSpace(credentials.ClientId) || string.IsNullOrWhiteSpace(credentials.ClientSecret))
        {
            throw new InvalidOperationException("Aprimo workbook generation requires SubDomain or BaseUrl, ClientId, and ClientSecret.");
        }

        var options = request.ExportOptions ?? AprimoConfigurationWorkbookExportOptions.Defaults;
        var token = await GetAccessTokenAsync(credentials, cancellationToken).ConfigureAwait(false);
        var client = new AprimoWorkbookApiClient(_httpClient, credentials.SubDomain.Trim(), token);
        var builder = new AprimoWorkbookDataBuilder(client, _logger, cancellationToken, credentials.SubDomain.Trim());
        var sheets = await builder.BuildSheetsAsync(options).ConfigureAwait(false);

        XlsxWorkbookWriter.Write(outputStream, sheets);

        var rowCounts = sheets.ToDictionary(x => x.Name, x => Math.Max(0, x.Rows.Count - 1), StringComparer.OrdinalIgnoreCase);
        return new AprimoConfigurationWorkbookResult(
            $"{credentials.SubDomain.Trim().ToUpperInvariant()} ConfigWorkbook {DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx",
            ContentType,
            sheets.Count,
            rowCounts);
    }

    private static AprimoConfigurationWorkbookCredentials NormalizeCredentials(AprimoConfigurationWorkbookCredentials credentials)
    {
        var subDomain = credentials.SubDomain;

        if (string.IsNullOrWhiteSpace(subDomain) && !string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            subDomain = ExtractSubDomain(credentials.BaseUrl);
        }

        return credentials with
        {
            SubDomain = subDomain?.Trim() ?? string.Empty,
            ClientId = credentials.ClientId?.Trim() ?? string.Empty,
            ClientSecret = credentials.ClientSecret?.Trim() ?? string.Empty,
            BaseUrl = credentials.BaseUrl?.Trim()
        };
    }

    private static string ExtractSubDomain(string baseUrl)
    {
        var value = baseUrl.Trim();
        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            value = "https://" + value;
        }

        var host = Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri.Host
            : baseUrl.Trim().Split('/')[0];

        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            host = host[4..];
        }

        if (host.EndsWith(".dam.aprimo.com", StringComparison.OrdinalIgnoreCase))
        {
            return host[..^".dam.aprimo.com".Length];
        }

        if (host.EndsWith(".aprimo.com", StringComparison.OrdinalIgnoreCase))
        {
            return host[..^".aprimo.com".Length];
        }

        return host.Split('.')[0];
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
        request.Headers.Add("User-Agent", "Azure Aprimo Migration Connector");

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
    private readonly string _subDomain;

    private Dictionary<string, JsonObject> _classifications = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, JsonObject> _fieldGroups = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, JsonObject> _fieldDefinitions = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, JsonObject> _userGroups = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, JsonObject> _watermarks = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, JsonObject> _contentTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IReadOnlyList<string>> _optionListRows = Start("ID", "Field Name", "Option Name", "Option Label", "Notes");

    public AprimoWorkbookDataBuilder(AprimoWorkbookApiClient client, ILogger logger, CancellationToken cancellationToken, string subDomain)
    {
        _client = client;
        _logger = logger;
        _cancellationToken = cancellationToken;
        _subDomain = subDomain;
    }

    public async Task<List<XlsxWorksheet>> BuildSheetsAsync(AprimoConfigurationWorkbookExportOptions options)
    {
        await PreloadAsync(options).ConfigureAwait(false);
        var sheets = new List<XlsxWorksheet> { CoverSheet(_subDomain) };

        if (options.UserGroups) sheets.Add(UserGroupsSheet());
        if (options.FieldGroups) sheets.Add(FieldGroupsSheet());
        if (options.FieldDefinitions)
        {
            sheets.Add(FieldDefinitionsSheet(options));
            if (_optionListRows.Count > 1)
            {
                sheets.Add(new XlsxWorksheet("Option List Items", _optionListRows));
            }
        }
        if (options.Classifications) sheets.Add(ClassificationsSheet(options));
        if (options.ClassificationPermissions) sheets.Add(await ClassificationPermissionsSheetAsync().ConfigureAwait(false));
        if (options.FunctionalPermissions) sheets.Add(await FunctionalPermissionsSheetAsync().ConfigureAwait(false));
        if (options.Settings) sheets.Add(await SettingsSheetAsync().ConfigureAwait(false));
        if (options.Watermarks) sheets.Add(WatermarksSheet());
        if (options.Translations) sheets.Add(await TranslationsSheetAsync(options).ConfigureAwait(false));
        if (options.Rules) sheets.Add(RulesSheet());
        if (options.ContentTypes) sheets.Add(ContentTypesSheet());

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
        if (options.FieldDefinitions || options.Classifications || options.Rules || options.ContentTypes)
        {
            _fieldDefinitions = await LoadDictionaryAsync("/fielddefinitions", null).ConfigureAwait(false);
        }
        if (options.UserGroups || options.FunctionalPermissions || options.ClassificationPermissions)
        {
            _userGroups = await LoadDictionaryAsync("/usergroups", null).ConfigureAwait(false);
        }
        if (options.Rules || options.Watermarks)
        {
            _watermarks = await LoadDictionaryAsync("/watermarks", null).ConfigureAwait(false);
        }
        if (options.ContentTypes)
        {
            _contentTypes = await LoadDictionaryAsync("/contenttypes", null).ConfigureAwait(false);
        }
    }

    private async Task<Dictionary<string, JsonObject>> LoadDictionaryAsync(string url, IDictionary<string, string>? headers)
    {
        var items = await _client.GetAllAsync(url, headers, _cancellationToken).ConfigureAwait(false);
        return items.Where(x => !string.IsNullOrWhiteSpace(GetText(x, "id")))
            .GroupBy(x => NormalizeId(GetText(x, "id")), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static XlsxWorksheet CoverSheet(string subDomain)
    {
        return new XlsxWorksheet("Cover Sheet", new List<IReadOnlyList<string>>
        {
            new[] { "Configuration Workbook" },
            new[] { "Environment", subDomain },
            new[] { "Generated UTC", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) }
        });
    }

    private XlsxWorksheet UserGroupsSheet()
    {
        var rows = Start("ID", "Name", "Notes");
        foreach (var item in _userGroups.Values.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new[] { GetText(item, "id"), GetText(item, "name"), string.Empty });
        }
        return new XlsxWorksheet("User Groups", rows);
    }

    private XlsxWorksheet FieldGroupsSheet()
    {
        var rows = Start("ID", "Name", "Notes");
        foreach (var item in _fieldGroups.Values.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new[] { GetText(item, "id"), GetText(item, "name"), string.Empty });
        }
        return new XlsxWorksheet("Field Groups", rows);
    }

    private XlsxWorksheet FieldDefinitionsSheet(AprimoConfigurationWorkbookExportOptions options)
    {
        var headers = new List<string> { "ID", "Name", "Label in English" };
        if (options.Languages) headers.AddRange(LanguageHeaders());
        headers.AddRange(new[] { "Sort index", "Data Type", "Scope", "Required", "Searchable", "Read only", "Unique Identifier", "Default Value", "Default Value Triggers", "Predicted field", "Hint", "Validation", "Validation message", "Field Groups", "Help text", "Additional Info", "Notes" });

        var rows = new List<IReadOnlyList<string>> { headers };
        foreach (var item in _fieldDefinitions.Values.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            var row = new List<string>
            {
                GetText(item, "id"),
                GetText(item, "name"),
                Label(item)
            };
            if (options.Languages) row.AddRange(LanguageLabels(item));
            row.AddRange(new[]
            {
                GetText(item, "sortIndex"),
                AddSpacesToSentenceCase(GetText(item, "dataType", "type", "fieldType")),
                AddSpacesToSentenceCase(GetText(item, "scope")),
                BoolText(item, "isRequired", "required"),
                BoolText(item, "indexed", "searchable"),
                BoolText(item, "isReadOnly", "readOnly"),
                BoolText(item, "isUniqueIdentifier", "uniqueIdentifier"),
                GetText(item, "defaultValue"),
                JsonArrayToString(item["resetToDefaultTriggers"]),
                BoolText(item, "metadataPredictionEnabled", "predictedField"),
                GetText(item, "hints", "hint"),
                GetText(item, "validation"),
                GetText(item, "validationErrorMessage"),
                JoinIds(item["memberships"], _fieldGroups),
                GetText(item, "helpText"),
                AdditionalFieldInfo(item),
                string.Empty
            });
            rows.Add(row);
        }
        return new XlsxWorksheet("Field Definitions", rows);
    }

    private string AdditionalFieldInfo(JsonObject fieldDefinition)
    {
        var dataType = GetText(fieldDefinition, "dataType", "type", "fieldType");
        if (dataType.Equals("OptionList", StringComparison.OrdinalIgnoreCase))
        {
            var fieldName = GetText(fieldDefinition, "name");
            AddOptionListRows(fieldName, fieldDefinition["items"]);
            return $"See 'Option List Items'-tab for list of available options and filter on 'Field Name' = {fieldName}";
        }

        if (dataType.Equals("ClassificationList", StringComparison.OrdinalIgnoreCase))
        {
            var rootId = NormalizeId(GetText(fieldDefinition, "rootId"));
            var label = string.IsNullOrWhiteSpace(rootId) || rootId.Equals("00000000000000000000000000000000", StringComparison.OrdinalIgnoreCase)
                ? "Top Level"
                : LookupName(_classifications, rootId, "namePath", "name", "id");
            var filter = GetText(fieldDefinition, "filter");
            var linkToSelected = BoolText(fieldDefinition, "linkRecordToSelectedClassifications");
            var multiselect = BoolText(fieldDefinition, "acceptMultipleOptions", "multiValue", "isMultiValue");
            return $"Uses '{label}' as root for showing available options, with filter '{filter}'. Link records to selected classifications: '{linkToSelected}', multi-select: '{multiselect}'";
        }

        return string.Empty;
    }

    private void AddOptionListRows(string fieldName, JsonNode? options)
    {
        if (options is not JsonArray arr) return;
        foreach (var option in arr.OfType<JsonObject>())
        {
            _optionListRows.Add(new[] { GetText(option, "id"), fieldName, GetText(option, "name"), GetText(option, "label", "value", "name"), string.Empty });
        }
    }

    private XlsxWorksheet ClassificationsSheet(AprimoConfigurationWorkbookExportOptions options)
    {
        var headers = new List<string> { "ID", "NamePath", "Name", "Label in English" };
        if (options.Languages) headers.AddRange(LanguageHeaders());
        headers.AddRange(new[] { "Identifier", "Registered Field Groups", "Registered Fields", "Notes" });
        var rows = new List<IReadOnlyList<string>> { headers };

        foreach (var item in _classifications.Values.OrderBy(x => GetText(x, "namePath", "name"), StringComparer.OrdinalIgnoreCase))
        {
            var row = new List<string> { GetText(item, "id"), GetText(item, "namePath"), GetText(item, "name"), Label(item) };
            if (options.Languages) row.AddRange(LanguageLabels(item));
            row.AddRange(new[] { GetText(item, "identifier"), JoinRegisteredObjects(item["registeredFieldGroups"], "fieldGroupId", _fieldGroups), JoinRegisteredObjects(item["registeredFields"], "fieldId", _fieldDefinitions), string.Empty });
            rows.Add(row);
        }
        return new XlsxWorksheet("Classifications", rows);
    }

    private async Task<XlsxWorksheet> ClassificationPermissionsSheetAsync()
    {
        var headers = new List<string> { "Path", "Permission Type", "Break Inheritance" };
        headers.AddRange(_userGroups.Values.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase).Select(x => GetText(x, "name")));
        headers.Add("Notes");
        var rows = new List<IReadOnlyList<string>> { headers };
        var userGroupNamesById = _userGroups.ToDictionary(x => NormalizeId(x.Key), x => GetText(x.Value, "name"), StringComparer.OrdinalIgnoreCase);

        foreach (var classification in _classifications.Values.OrderBy(x => GetText(x, "namePath", "name"), StringComparer.OrdinalIgnoreCase))
        {
            var id = GetText(classification, "id");
            if (string.IsNullOrWhiteSpace(id)) continue;
            var path = GetText(classification, "namePath", "name");

            foreach (var permissionType in new[] { "ClassificationTreePermissions", "RecordPermissions", "DownloadPermissions" })
            {
                try
                {
                    var permission = await _client.GetObjectAsync($"/classification/{id}/{permissionType}", null, _cancellationToken).ConfigureAwait(false);
                    var permissions = permission["permissions"] as JsonArray;
                    var breakInheritance = BoolText(permission, "breakInheritance");
                    if ((permissions is null || permissions.Count == 0) && !breakInheritance.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var row = headers.Select(_ => string.Empty).ToList();
                    row[0] = path;
                    row[1] = permissionType.Replace("Permissions", string.Empty, StringComparison.OrdinalIgnoreCase);
                    row[2] = breakInheritance;
                    if (permissions is not null)
                    {
                        foreach (var item in permissions.OfType<JsonObject>())
                        {
                            var userGroupId = NormalizeId(GetText(item, "userGroupId"));
                            if (!userGroupNamesById.TryGetValue(userGroupId, out var userGroupName)) continue;
                            var columnIndex = headers.FindIndex(x => x.Equals(userGroupName, StringComparison.OrdinalIgnoreCase));
                            if (columnIndex >= 0) row[columnIndex] = AddSpacesToSentenceCase(GetText(item, "accessRight"));
                        }
                    }
                    rows.Add(row);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed reading Aprimo classification permission {PermissionType} for {ClassificationId}.", permissionType, id);
                }
            }
        }
        return new XlsxWorksheet("Classification Permissions", rows);
    }

    private async Task<XlsxWorksheet> FunctionalPermissionsSheetAsync()
    {
        var permissions = await _client.GetAllAsync("/permissions", null, _cancellationToken).ConfigureAwait(false);
        var userGroups = _userGroups.Values.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase).ToList();
        var headers = new List<string> { "Functional Permission Name", "Functional Permission Label" };
        headers.AddRange(userGroups.Select(x => GetText(x, "name")));
        headers.Add("Notes");
        var rows = new List<IReadOnlyList<string>> { headers };

        var permissionRows = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var permission in permissions.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            var name = GetText(permission, "name");
            if (string.IsNullOrWhiteSpace(name)) continue;
            var row = headers.Select(_ => string.Empty).ToList();
            row[0] = name;
            row[1] = Label(permission);
            permissionRows[name] = row;
            rows.Add(row);
        }

        foreach (var userGroup in userGroups)
        {
            var userGroupName = GetText(userGroup, "name");
            var userGroupId = GetText(userGroup, "id");
            if (string.IsNullOrWhiteSpace(userGroupId)) continue;
            var columnIndex = headers.FindIndex(x => x.Equals(userGroupName, StringComparison.OrdinalIgnoreCase));
            if (columnIndex < 0) continue;

            try
            {
                var userGroupPermissions = await _client.GetAllAsync($"/usergroup/{userGroupId}/permissions", null, _cancellationToken).ConfigureAwait(false);
                foreach (var permission in userGroupPermissions)
                {
                    var name = GetText(permission, "name");
                    if (!permissionRows.TryGetValue(name, out var row)) continue;
                    var value = GetText(permission, "value");
                    if (!value.Equals("None", StringComparison.OrdinalIgnoreCase))
                    {
                        row[columnIndex] = AddSpacesToSentenceCase(value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed reading Aprimo functional permissions for user group {UserGroupId}.", userGroupId);
            }
        }

        return new XlsxWorksheet("Functional Permissions", rows);
    }

    private async Task<XlsxWorksheet> SettingsSheetAsync()
    {
        var definitions = await _client.GetAllAsync("/settingdefinitions", null, _cancellationToken).ConfigureAwait(false);
        var rows = Start("ID", "Name", "Label", "Value", "Notes");

        foreach (var definition in definitions.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            var name = GetText(definition, "name");
            if (string.IsNullOrWhiteSpace(name)) continue;

            var dataType = GetText(definition, "dataType");
            var userGroupSettingMode = GetText(definition, "userGroupSettingMode");
            if (dataType.Equals("ROLE", StringComparison.OrdinalIgnoreCase) || userGroupSettingMode.Equals("MANUAL", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var setting = await _client.GetObjectAsync($"/setting/{Uri.EscapeDataString(name)}", null, _cancellationToken).ConfigureAwait(false);
                var value = GetText(setting, "value");
                var defaultValue = GetText(definition, "defaultValue");
                if (!string.IsNullOrWhiteSpace(value) && !value.Equals(defaultValue, StringComparison.Ordinal))
                {
                    rows.Add(new[] { GetText(definition, "id"), name, Label(definition), value, string.Empty });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed reading Aprimo setting value for {SettingName}.", name);
            }
        }

        return new XlsxWorksheet("Settings", rows);
    }

    private XlsxWorksheet WatermarksSheet()
    {
        var rows = Start("ID", "Name", "Position", "Notes");
        foreach (var item in _watermarks.Values.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new[] { GetText(item, "id"), GetText(item, "name"), GetText(item, "position"), string.Empty });
        }
        return new XlsxWorksheet("Watermarks", rows);
    }

    private async Task<XlsxWorksheet> TranslationsSheetAsync(AprimoConfigurationWorkbookExportOptions options)
    {
        var items = await _client.GetAllAsync("/translations", null, _cancellationToken).ConfigureAwait(false);
        var headers = new List<string> { "ID", "Studio", "Module", "Name", "Label in English" };
        if (options.Languages) headers.AddRange(LanguageHeaders());
        headers.Add("Notes");
        var rows = new List<IReadOnlyList<string>> { headers };
        foreach (var item in items.OrderBy(x => GetText(x, "studio") + GetText(x, "module") + GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            var row = new List<string> { GetText(item, "id"), GetText(item, "studio"), GetText(item, "module"), GetText(item, "name"), FindLabel(item["localizedValues"]) };
            if (options.Languages) row.AddRange(LanguageLabels(item["localizedValues"]));
            row.Add(string.Empty);
            rows.Add(row);
        }
        return new XlsxWorksheet("Translations", rows);
    }

    private XlsxWorksheet RulesSheet()
    {
        var rows = Start("ID", "Name", "Target", "Trigger", "Enabled", "Enabled on drafts", "Conditions", "Actions", "Notes");
        foreach (var item in _client.GetAllAsync("/rules", new Dictionary<string, string> { ["select-Rule"] = "conditions, actions" }, _cancellationToken).GetAwaiter().GetResult().OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new[]
            {
                GetText(item, "id"),
                GetText(item, "name"),
                GetText(item, "target"),
                AddSpacesToSentenceCase(GetText(item, "trigger")),
                BoolText(item, "enabled"),
                BoolText(item, "includeDraftRecords"),
                RuleConditionsText(item["_embedded"]?["conditions"]?["items"]),
                RuleActionsText(item["_embedded"]?["actions"]?["items"]),
                string.Empty
            });
        }
        return new XlsxWorksheet("Rules", rows);
    }

    private XlsxWorksheet ContentTypesSheet()
    {
        var rows = Start("ID", "Name", "Purpose", "Parent", "Fields", "File extensions", "Title config", "Title field (not)", "Inheritance", "Inheritance link field", "Inheritable fields", "Notes");
        foreach (var item in _contentTypes.Values.OrderBy(x => GetText(x, "name"), StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new[]
            {
                GetText(item, "id"),
                GetText(item, "name"),
                GetText(item, "purpose"),
                GetText(item, "parentId"),
                JoinRegisteredObjects(item["registeredFields"], "fieldId", _fieldDefinitions),
                JsonArrayToString(item["defaultFileExtensions"]),
                GetText(item, "titleConfiguration"),
                string.Empty,
                GetText(item, "inheritanceConfiguration"),
                GetText(item, "inheritanceFieldId"),
                JoinRegisteredObjects(item["inheritableFields"], "fieldId", _fieldDefinitions),
                string.Empty
            });
        }
        return new XlsxWorksheet("Content types", rows);
    }

    private static List<IReadOnlyList<string>> Start(params string[] headers) => new() { headers };
    private static string Raw(JsonNode? node) => AprimoConfigurationWorkbookService.Serialize(node);
    private static string GetText(JsonObject obj, params string[] keys) => AprimoConfigurationWorkbookService.Text(obj, keys);
    private static string Label(JsonObject obj) => FindLabel(obj["labels"]);
    private static string BoolText(JsonObject obj, params string[] keys) => keys.Select(k => obj[k]).OfType<JsonValue>().Select(v => v.ToString()).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
    private static string[] LanguageHeaders() => new[] { "Label in Spanish", "Label in French", "Label in German", "Label in Italian", "Label in Japanese", "Label in Simplified Chinese", "Label in Brazilian Portuguese" };
    private static string[] LanguageLabels(JsonObject obj) => LanguageLabels(obj["labels"]);
    private static string[] LanguageLabels(JsonNode? labels) => new[] { "4877d795-541c-4fac-86b9-9a06830b08ee", "18ca1f44-0c21-4eed-aecc-b98f0ae65a81", "691a7c86-57dc-4330-a512-c616db954d6a", "5ec55442-1cac-4734-a0ed-e1d01cd1e49f", "e6665327-1314-43c0-924f-9366e77064d7", "bcda9def-f15e-4ab2-90d1-99f2d0428f5e", "1e498386-288d-41d0-9e5b-beb8580ffd73" }.Select(id => FindLabel(labels, id)).ToArray();
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

    private static string JoinIds(JsonNode? values, Dictionary<string, JsonObject> lookup)
    {
        if (values is not JsonArray arr) return string.Empty;
        var names = new List<string>();
        foreach (var node in arr)
        {
            var id = NodeId(node);
            if (string.IsNullOrWhiteSpace(id)) continue;
            names.Add(LookupName(lookup, id, "name", "namePath", "id"));
        }
        return string.Join(", ", names.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string JoinRegisteredObjects(JsonNode? values, string idProperty, Dictionary<string, JsonObject> lookup)
    {
        if (values is not JsonArray arr) return string.Empty;
        var names = new List<string>();
        foreach (var node in arr)
        {
            var id = node switch
            {
                JsonObject obj => GetText(obj, idProperty, "id", "fieldId", "fieldGroupId"),
                JsonValue value => value.ToString(),
                _ => string.Empty
            };
            if (string.IsNullOrWhiteSpace(id)) continue;
            names.Add(LookupName(lookup, id, "name", "namePath", "id"));
        }
        return string.Join(", ", names.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string LookupName(Dictionary<string, JsonObject> lookup, string id, params string[] fields)
    {
        var key = NormalizeId(id);
        return lookup.TryGetValue(key, out var item) ? GetText(item, fields) : id;
    }

    private static string NodeId(JsonNode? node) => node switch
    {
        JsonValue value => value.ToString(),
        JsonObject obj => GetText(obj, "id"),
        _ => string.Empty
    };

    private static string JsonArrayToString(JsonNode? values)
    {
        if (values is not JsonArray arr) return string.Empty;
        return string.Join(", ", arr.Select(x => x switch
        {
            JsonValue value => value.ToString(),
            JsonObject obj => GetText(obj, "name", "id"),
            _ => string.Empty
        }).Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string RuleConditionsText(JsonNode? values)
    {
        if (values is not JsonArray arr) return string.Empty;
        var texts = new List<string>();
        foreach (var condition in arr.OfType<JsonObject>())
        {
            var type = GetText(condition, "conditionType");
            switch (type.ToUpperInvariant())
            {
                case "CONTENTTYPEIS": texts.Add($"if the content type is {GetText(condition, "contentType")}"); break;
                case "CLASSIFIEDIN": texts.Add($"if the record is classified in {LookupNameFromProperty(condition, "classificationId", null)}"); break;
                case "FIELDVALUECHANGED": texts.Add($"if field {LookupNameFromProperty(condition, "fieldDefinitionId", null)} changed"); break;
                case "RECORDSTATUSCHANGED": texts.Add("the record status has changed"); break;
                case "MASTERPREVIEWEXISTS": texts.Add("the record has a master preview"); break;
                case "MASTERPREVIEWCHANGED": texts.Add("the record has a new master preview"); break;
                default: texts.Add(AddSpacesToSentenceCase(type)); break;
            }
        }
        return string.Join("\n", texts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private string RuleActionsText(JsonNode? values)
    {
        if (values is not JsonArray arr) return string.Empty;
        var texts = new List<string>();
        foreach (var action in arr.OfType<JsonObject>())
        {
            var type = GetText(action, "actionType");
            switch (type.ToUpperInvariant())
            {
                case "REFRESHFILES": texts.Add("recreate the preview(s) of the record"); break;
                case "REFERENCE": texts.Add($"execute the following reference:\n{GetText(action, "reference")}"); break;
                case "APPLYWATERMARKONMASTERFILE":
                    var watermarkType = GetText(action, "watermarkType");
                    if (watermarkType.Equals("None", StringComparison.OrdinalIgnoreCase)) texts.Add("Remove watermark on the master file of the record");
                    else if (watermarkType.Equals("UseSetting", StringComparison.OrdinalIgnoreCase)) texts.Add("apply the watermark specified in the user's .watermarkName-setting on the master file of the record");
                    else texts.Add($"apply '{LookupName(_watermarks, GetText(action, "watermarkId"), "name", "id")}' watermark on the master file of the record");
                    break;
                case "CLASSIFYRECORD": texts.Add($"classify record in '{LookupName(_classifications, GetText(action, "classificationId"), "namePath", "name", "id")}'"); break;
                case "UNCLASSIFYRECORD": texts.Add("unlink record from classification(s) " + JoinIds(action["classificationIds"], _classifications)); break;
                case "SCHEDULERESAVEOFRECORD": texts.Add($"schedule the record to be resaved on the date specified in {LookupName(_fieldDefinitions, GetText(action, "fieldDefinitionId"), "name", "id")}"); break;
                case "SETFIELDVALUE": texts.Add($"set field {LookupName(_fieldDefinitions, GetText(action, "fieldDefinitionId"), "name", "id")}"); break;
                default: texts.Add(AddSpacesToSentenceCase(type)); break;
            }
        }
        return string.Join("\n", texts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string LookupNameFromProperty(JsonObject obj, string propertyName, Dictionary<string, JsonObject>? lookup)
    {
        var id = GetText(obj, propertyName);
        return lookup is null ? id : LookupName(lookup, id, "name", "namePath", "id");
    }

    private static string NormalizeId(string value) => value.Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

    private static string AddSpacesToSentenceCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var spaced = Regex.Replace(value, "(?<=[a-z0-9])(?=[A-Z])", " ");
        return spaced.Trim();
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
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font></fonts><fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF005F7F"/><bgColor indexed="64"/></patternFill></fill></fills><borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/></cellXfs></styleSheet>
""";
    private static string Worksheet(XlsxWorksheet sheet)
    {
        var sb = new StringBuilder();
        var maxCols = Math.Max(1, sheet.Rows.Select(x => x.Count).DefaultIfEmpty(1).Max());
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
        sb.Append($"</sheetData><autoFilter ref=\"A1:{ColumnName(maxCols)}1\"/></worksheet>");
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
