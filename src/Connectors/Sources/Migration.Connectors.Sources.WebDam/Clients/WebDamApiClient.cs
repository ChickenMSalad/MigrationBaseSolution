using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Migration.Connectors.Sources.WebDam.Configuration;
using Migration.Connectors.Sources.WebDam.Models;

namespace Migration.Connectors.Sources.WebDam.Clients;

public sealed class WebDamApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly WebDamAuthClient _authClient;
    private readonly WebDamOptions _options;

    public WebDamApiClient(
        HttpClient httpClient,
        WebDamAuthClient authClient,
        IOptions<WebDamOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);
        }
    }

    public async Task<IReadOnlyList<WebDamFolderDto>> GetTopLevelFoldersAsync(CancellationToken cancellationToken = default)
        => await SendJsonAsync<List<WebDamFolderDto>>(HttpMethod.Get, "folders", cancellationToken).ConfigureAwait(false)
           ?? new List<WebDamFolderDto>();

    public async Task<IReadOnlyList<WebDamFolderDto>> GetAllFoldersAsync(CancellationToken cancellationToken = default)
    {
        var folders = await SendJsonAsync<List<WebDamFolderDto>>(
            HttpMethod.Get,
            "folders",
            cancellationToken).ConfigureAwait(false);

        return folders ?? new List<WebDamFolderDto>();
    }

    public async Task<JsonDocument> GetFolderInfoRawAsync(
    string folderId,
    CancellationToken cancellationToken = default)
    {
        return await SendJsonDocumentAsync(
            HttpMethod.Get,
            $"folders/{Uri.EscapeDataString(folderId)}",
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<WebDamFolderAssetsResponse> GetFolderContentsAsync(
        string folderId,
        int offset,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var pageSize = limit ?? _options.PageSize;
        var path = $"folders/{Uri.EscapeDataString(folderId)}/assets?limit={pageSize}&offset={offset}";

        //var resp = await SendJsonAsync<WebDamFolderAssetsResponse>(HttpMethod.Get, path, cancellationToken)
        //       .ConfigureAwait(false)
        //       ?? new WebDamFolderAssetsResponse();

        ;
        return await SendJsonAsync<WebDamFolderAssetsResponse>(HttpMethod.Get, path, cancellationToken)
               .ConfigureAwait(false)
               ?? new WebDamFolderAssetsResponse();
    }

    public async Task<WebDamAssetDto> GetAssetAsync(string assetId, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<WebDamAssetDto>(
                   HttpMethod.Get,
                   $"assets/{Uri.EscapeDataString(assetId)}",
                   cancellationToken)
               .ConfigureAwait(false)
               ?? throw new WebDamException($"Asset '{assetId}' returned no payload.");
    }

    private static void ParseMetadataCollection(
    JsonElement element,
    HashSet<string> requestedIds,
    Dictionary<string, Dictionary<string, string>> result)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = TryGetPropertyAsString(item, "id")
                     ?? TryGetPropertyAsString(item, "assetid");

            if (!string.IsNullOrWhiteSpace(id) && requestedIds.Contains(id))
            {
                result[id] = FlattenMetadataObject(item);
                continue;
            }

            // Some payloads may be shaped like:
            // { "165459198": { ...metadata... } }
            foreach (var prop in item.EnumerateObject())
            {
                if (requestedIds.Contains(prop.Name) && prop.Value.ValueKind == JsonValueKind.Object)
                {
                    result[prop.Name] = FlattenMetadataObject(prop.Value);
                }
            }
        }
    }

    private static void ExpandActiveFields(
        JsonElement element,
        IDictionary<string, string> output)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                AddMetadataValue(output, property.Name, JsonElementToMetadataString(property.Value));
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            var raw = element.GetString();

            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(raw);
                ExpandActiveFields(doc.RootElement, output);
            }
            catch
            {
                // ignore non-json strings
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var fieldName = TryGetPropertyAsString(item, "field")
                                ?? TryGetPropertyAsString(item, "name")
                                ?? TryGetPropertyAsString(item, "key");

                if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    if (TryGetProperty(item, "value", out var valueElement))
                    {
                        AddMetadataValue(output, fieldName, JsonElementToMetadataString(valueElement));
                    }
                    else if (TryGetProperty(item, "values", out var valuesElement))
                    {
                        AddMetadataValue(output, fieldName, JsonElementToMetadataString(valuesElement));
                    }
                    else
                    {
                        // If there is no obvious "value", flatten the rest of the object
                        foreach (var property in item.EnumerateObject())
                        {
                            if (string.Equals(property.Name, "field", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(property.Name, "name", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(property.Name, "key", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            AddMetadataValue(output, fieldName, JsonElementToMetadataString(property.Value));
                        }
                    }

                    continue;
                }

                foreach (var property in item.EnumerateObject())
                {
                    AddMetadataValue(output, property.Name, JsonElementToMetadataString(property.Value));
                }
            }
        }
    }
    private static Dictionary<string, string> FlattenMetadataObject(JsonElement element)
    {
        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (element.ValueKind != JsonValueKind.Object)
        {
            return output;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, "type", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property.Name, "assetid", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(property.Name, "active_fields", StringComparison.OrdinalIgnoreCase))
            {
                ExpandActiveFields(property.Value, output);
                continue;
            }

            AddMetadataValue(output, property.Name, JsonElementToMetadataString(property.Value));
        }

        return output;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> GetXmpMetadataBatchAsync(
        IReadOnlyCollection<string> assetIds,
        CancellationToken cancellationToken = default)
    {
        if (assetIds.Count == 0)
        {
            return new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        if (assetIds.Count > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(assetIds), "WebDam XMP batch reads are limited to 50 IDs.");
        }

        var requestedIds = new HashSet<string>(assetIds, StringComparer.OrdinalIgnoreCase);
        var joined = string.Join(",", assetIds);

        using var document = await SendJsonDocumentAsync(
            HttpMethod.Get,
            $"assets/{joined}/metadatas/xmp",
            cancellationToken).ConfigureAwait(false);

        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Object)
        {
            // Case 1: batch response keyed by asset id:
            // {
            //   "165459198": { ... },
            //   "165459201": { ... }
            // }
            var matchedAssetKeys = 0;

            foreach (var prop in root.EnumerateObject())
            {
                if (requestedIds.Contains(prop.Name) && prop.Value.ValueKind == JsonValueKind.Object)
                {
                    result[prop.Name] = FlattenMetadataObject(prop.Value);
                    matchedAssetKeys++;
                }
            }

            if (matchedAssetKeys > 0)
            {
                return result;
            }

            // Case 2: wrapper object containing an array
            foreach (var wrapperName in new[] { "items", "assets", "metadatas", "metadata", "data" })
            {
                if (TryGetProperty(root, wrapperName, out var wrapperElement))
                {
                    ParseMetadataCollection(wrapperElement, requestedIds, result);
                    if (result.Count > 0)
                    {
                        return result;
                    }
                }
            }

            // Case 3: single asset response
            // Example:
            // {
            //   "type": "assetxmp",
            //   "headline": "...",
            //   ...
            // }
            if (assetIds.Count == 1)
            {
                var singleId = assetIds.First();
                result[singleId] = FlattenMetadataObject(root);
                return result;
            }

            // Case 4: object with id/assetid and metadata fields
            var idFromObject = TryGetPropertyAsString(root, "id")
                               ?? TryGetPropertyAsString(root, "assetid");

            if (!string.IsNullOrWhiteSpace(idFromObject) && requestedIds.Contains(idFromObject))
            {
                result[idFromObject] = FlattenMetadataObject(root);
                return result;
            }

            return result;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            ParseMetadataCollection(root, requestedIds, result);

            // Fallback: if the array elements do not include ids, map by request order
            if (result.Count == 0)
            {
                var requestedList = assetIds.ToList();
                var index = 0;

                foreach (var item in root.EnumerateArray())
                {
                    if (index >= requestedList.Count)
                    {
                        break;
                    }

                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        result[requestedList[index]] = FlattenMetadataObject(item);
                        index++;
                    }
                }
            }

            return result;
        }

        return result;
    }

    public async Task<IReadOnlyList<WebDamXmpSchemaFieldDto>> GetXmpSchemaAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendJsonAsync<WebDamXmpSchemaResponse>(
            HttpMethod.Get,
            "metadataschemas/xmp?full=1",
            cancellationToken).ConfigureAwait(false);

        return response?.XmpSchema ?? new List<WebDamXmpSchemaFieldDto>();
    }

    public async Task<Stream> DownloadAssetAsync(string assetId, CancellationToken cancellationToken = default)
    {
        var request = await CreateAuthorizedRequestAsync(
            HttpMethod.Get,
            $"assets/{Uri.EscapeDataString(assetId)}/download",
            cancellationToken).ConfigureAwait(false);

        var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            response.Dispose();

            throw new WebDamException($"Asset download failed for asset '{assetId}'.")
            {
                StatusCode = response.StatusCode,
                ResponseBody = body
            };
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<T?> SendJsonAsync<T>(
        HttpMethod method,
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        using var request = await CreateAuthorizedRequestAsync(method, relativeUrl, cancellationToken).ConfigureAwait(false);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new WebDamException($"WebDam request failed for '{relativeUrl}'.")
            {
                StatusCode = response.StatusCode,
                ResponseBody = body
            };
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    private async Task<JsonDocument> SendJsonDocumentAsync(
        HttpMethod method,
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        using var request = await CreateAuthorizedRequestAsync(method, relativeUrl, cancellationToken).ConfigureAwait(false);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new WebDamException($"WebDam request failed for '{relativeUrl}'.")
            {
                StatusCode = response.StatusCode,
                ResponseBody = body
            };
        }

        return JsonDocument.Parse(body);
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(
        HttpMethod method,
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        var token = await _authClient.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        var request = new HttpRequestMessage(method, relativeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static Dictionary<string, string> FlattenObject(JsonElement element)
    {
        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (element.ValueKind != JsonValueKind.Object)
        {
            return output;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, "type", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property.Name, "assetid", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AddMetadataValue(output, property.Name, JsonElementToMetadataString(property.Value));
        }

        return output;
    }

    private static string JsonElementToString(JsonElement value)
    {
        return JsonElementToMetadataString(value);
    }

    private static string? TryGetPropertyAsString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return prop.Value.ValueKind == JsonValueKind.String
                    ? prop.Value.GetString()
                    : prop.Value.GetRawText();
            }
        }

        return null;
    }

    public static long? ParseSizeToBytes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerBytes))
        {
            return integerBytes;
        }

        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return (long)Math.Round(decimalValue, MidpointRounding.AwayFromZero);
        }

        return null;
    }

    private static void AddMetadataValue(
        IDictionary<string, string> output,
        string key,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalizedKey = key.Trim();
        var normalizedValue = value.Trim();

        var shouldTreatAsMultiValue =
            string.Equals(normalizedKey, "keyword", StringComparison.OrdinalIgnoreCase) ||
            normalizedKey.Contains("keyword", StringComparison.OrdinalIgnoreCase) ||
            normalizedKey.Contains("subject", StringComparison.OrdinalIgnoreCase) ||
            normalizedKey.Contains("tag", StringComparison.OrdinalIgnoreCase);

        if (output.TryGetValue(normalizedKey, out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            var existingValues = shouldTreatAsMultiValue
                ? SplitMetadataValuesForKeywords(existing)
                : SplitMetadataValues(existing);

            var incomingValues = shouldTreatAsMultiValue
                ? SplitMetadataValuesForKeywords(normalizedValue)
                : SplitMetadataValues(normalizedValue);

            var merged = existingValues
                .Concat(incomingValues)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            output[normalizedKey] = string.Join("; ", merged);
            return;
        }

        if (shouldTreatAsMultiValue)
        {
            var values = SplitMetadataValuesForKeywords(normalizedValue);
            output[normalizedKey] = string.Join("; ", values);
            return;
        }

        output[normalizedKey] = normalizedValue;
    }

    private static List<string> SplitMetadataValuesForKeywords(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        return value
            .Split(new[] { ",", ";", "|" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    private static List<string> SplitMetadataValues(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        return value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string JsonElementToMetadataString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Array => string.Join(
                "; ",
                value.EnumerateArray()
                    .Select(JsonElementToMetadataString)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)),
            JsonValueKind.Object => TryExtractObjectValue(value),
            _ => value.ToString()
        };
    }

    private static string TryExtractObjectValue(JsonElement value)
    {
        // Common pattern: { "value": "abc" } or { "label": "abc" } or { "name": "abc" }
        foreach (var preferred in new[] { "value", "label", "name" })
        {
            if (TryGetProperty(value, preferred, out var inner))
            {
                return JsonElementToMetadataString(inner);
            }
        }

        // Fallback to raw JSON if there isn't a simple value shape
        return value.GetRawText();
    }
}
