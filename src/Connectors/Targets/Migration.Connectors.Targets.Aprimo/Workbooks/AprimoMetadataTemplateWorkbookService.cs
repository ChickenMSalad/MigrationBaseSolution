using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Migration.Connectors.Targets.Aprimo.Workbooks;

public sealed class AprimoMetadataTemplateWorkbookService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public AprimoMetadataTemplateWorkbookService(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AprimoMetadataTemplateWorkbookResult> GenerateAsync(
        AprimoConfigurationWorkbookCredentials credentials,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(outputStream);

        var normalized = NormalizeCredentials(credentials);
        if (string.IsNullOrWhiteSpace(normalized.SubDomain)
            || string.IsNullOrWhiteSpace(normalized.ClientId)
            || string.IsNullOrWhiteSpace(normalized.ClientSecret))
        {
            throw new InvalidOperationException("Aprimo metadata template generation requires SubDomain or BaseUrl, ClientId, and ClientSecret.");
        }

        var token = await GetAccessTokenAsync(normalized, cancellationToken).ConfigureAwait(false);
        var client = new AprimoTemplateApiClient(_httpClient, normalized.SubDomain.Trim(), token);
        var fieldDefinitions = await LoadFieldDefinitionsAsync(client, cancellationToken).ConfigureAwait(false);

        var metadataHeaders = fieldDefinitions
            .Select(GetTemplateColumnName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (metadataHeaders.Count == 0)
        {
            metadataHeaders.Add("Asset file name");
        }

        var templateRows = new List<IReadOnlyList<string>>
        {
            metadataHeaders
        };

        var definitionRows = new List<IReadOnlyList<string>>
        {
            new[]
            {
                "Id",
                "Name",
                "Label",
                "DataType",
                "Required",
                "MultiValue",
                "FieldGroup",
                "RawJson"
            }
        };

        foreach (var field in fieldDefinitions.OrderBy(GetTemplateColumnName, StringComparer.OrdinalIgnoreCase))
        {
            definitionRows.Add(new[]
            {
                Text(field, "id"),
                Text(field, "name"),
                Label(field),
                Text(field, "dataType", "type", "fieldType"),
                BoolText(field, "required", "isRequired"),
                BoolText(field, "multiValue", "isMultiValue"),
                JoinFieldGroups(field),
                AprimoConfigurationWorkbookService.Serialize(field)
            });
        }

        var sheets = new List<XlsxWorksheet>
        {
            new("Metadata Template", templateRows),
            new("Field Definitions", definitionRows)
        };

        XlsxWorkbookWriter.Write(outputStream, sheets);

        return new AprimoMetadataTemplateWorkbookResult(
            $"{normalized.SubDomain.Trim().ToUpperInvariant()} BlankMetadataTemplate {DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx",
            AprimoConfigurationWorkbookService.ContentType,
            fieldDefinitions.Count,
            metadataHeaders.Count);
    }

    private static async Task<IReadOnlyList<JsonObject>> LoadFieldDefinitionsAsync(
        AprimoTemplateApiClient client,
        CancellationToken cancellationToken)
    {
        var items = await client.GetAllAsync("/fielddefinitions", null, cancellationToken).ConfigureAwait(false);
        if (items.Count > 0)
        {
            return items;
        }

        return Array.Empty<JsonObject>();
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
            throw new HttpRequestException($"Aprimo token request failed: {(int)response.StatusCode} {response.ReasonPhrase}.{Environment.NewLine}{json}");
        }

        var root = JsonNode.Parse(json)?.AsObject() ?? throw new JsonException("Aprimo token response was empty.");
        var token = Text(root, "access_token", "accessToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new JsonException("Aprimo token response did not contain access_token.");
        }

        return token;
    }

    private static string GetTemplateColumnName(JsonObject field)
    {
        var label = Label(field);
        if (!string.IsNullOrWhiteSpace(label))
        {
            return label.Trim();
        }

        var name = Text(field, "name");
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim();
        }

        return Text(field, "id").Trim();
    }

    private static string Label(JsonObject obj) => FindLabel(obj["labels"]);

    private static string FindLabel(JsonNode? labels, string languageId = "c2bd4f9b-bb95-4bcb-80c3-1e924c9c26dc")
    {
        if (labels is not JsonArray arr)
        {
            return string.Empty;
        }

        foreach (var item in arr.OfType<JsonObject>())
        {
            if (string.Equals(Text(item, "languageId", "language"), languageId, StringComparison.OrdinalIgnoreCase))
            {
                return Text(item, "value", "label", "name");
            }
        }

        return arr.OfType<JsonObject>()
            .Select(x => Text(x, "value", "label", "name"))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
    }

    private static string BoolText(JsonObject obj, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (obj[key] is JsonValue value)
            {
                var text = value.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return string.Empty;
    }

    private static string JoinFieldGroups(JsonObject field)
    {
        var node = field["fieldGroups"] ?? field["registeredFieldGroups"];
        if (node is not JsonArray arr)
        {
            return string.Empty;
        }

        var names = new List<string>();
        foreach (var item in arr)
        {
            if (item is JsonValue value)
            {
                names.Add(value.ToString());
            }
            else if (item is JsonObject obj)
            {
                var text = Text(obj, "name", "id");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    names.Add(text);
                }
            }
        }

        return string.Join(", ", names.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string Text(JsonObject obj, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (obj[key] is JsonValue value)
            {
                var text = value.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return string.Empty;
    }
}

public sealed record AprimoMetadataTemplateWorkbookResult(
    string FileName,
    string ContentType,
    int FieldCount,
    int ColumnCount);

internal sealed class AprimoTemplateApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _damBaseUrl;
    private readonly string _token;

    public AprimoTemplateApiClient(HttpClient httpClient, string subDomain, string token)
    {
        _httpClient = httpClient;
        _damBaseUrl = $"https://{subDomain}.dam.aprimo.com/api/core";
        _token = token;
    }

    public async Task<IReadOnlyList<JsonObject>> GetAllAsync(
        string relativeUrl,
        IDictionary<string, string>? headers,
        CancellationToken cancellationToken)
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

    private async Task<JsonObject> GetObjectAsync(string relativeOrAbsoluteUrl, IDictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        var url = relativeOrAbsoluteUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? relativeOrAbsoluteUrl
            : CombineUrl(_damBaseUrl, relativeOrAbsoluteUrl);

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
            throw new HttpRequestException($"Aprimo GET failed: {(int)response.StatusCode} {response.ReasonPhrase}. Url='{url}'.{Environment.NewLine}{json}");
        }

        return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
    }

    private static string CombineUrl(string baseUrl, string path) => baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
}
