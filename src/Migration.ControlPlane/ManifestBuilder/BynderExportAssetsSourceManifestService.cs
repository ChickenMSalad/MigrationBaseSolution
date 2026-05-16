using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Migration.ControlPlane.Services;
using Microsoft.Extensions.Configuration;

namespace Migration.ControlPlane.ManifestBuilder;

public sealed class BynderExportAssetsSourceManifestService : ISourceManifestService
{
    private const string Source = "Bynder";
    private const string Service = "ExportAssets";

    private readonly IConfiguration _configuration;
    private readonly ICredentialResolver _credentialResolver;
    private readonly IHttpClientFactory _httpClientFactory;

    public BynderExportAssetsSourceManifestService(
        IConfiguration configuration,
        ICredentialResolver credentialResolver,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _credentialResolver = credentialResolver ?? throw new ArgumentNullException(nameof(credentialResolver));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public string SourceType => Source;

    public string ServiceName => Service;

    public ManifestBuilderServiceDescriptor GetDescriptor()
    {
        return new ManifestBuilderServiceDescriptor(
            Source,
            Service,
            "Bynder asset export manifest",
            "Builds a Bynder manifest by exporting all assets.",
            Array.Empty<ManifestBuilderOptionDescriptor>());
    }

    public async Task<BuildSourceManifestResult> BuildAsync(
        BuildSourceManifestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var requestOptions = request.Options ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var credentialValues = await ResolveCredentialValuesAsync(request.CredentialSetId, cancellationToken).ConfigureAwait(false);
        var options = BynderManifestBuildOptions.From(requestOptions, credentialValues, _configuration);

        using var http = _httpClientFactory.CreateClient();
        http.Timeout = TimeSpan.FromMinutes(10);
        http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

        var bearerToken = options.AccessToken;

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            bearerToken = await RequestAccessTokenAsync(http, options, cancellationToken).ConfigureAwait(false);
        }

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");

        var rows = new List<BynderManifestRow>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var page = 1;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var assets = await QueryAssetsAsync(http, options, page, cancellationToken).ConfigureAwait(false);

            if (assets.Count == 0)
            {
                break;
            }

            foreach (var asset in assets)
            {
                var id = asset.Id;
                if (!string.IsNullOrWhiteSpace(id) && !seenIds.Add(id))
                {
                    continue;
                }

                rows.Add(BynderManifestRow.From(rows.Count + 1, asset));
            }

            if (assets.Count < options.PageSize)
            {
                break;
            }

            page++;
        }

        var csv = BuildCsv(rows);
        var fileName = $"bynder-export-assets-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";

        return new BuildSourceManifestResult(
            Source,
            Service,
            fileName,
            "text/csv",
            csv,
            ContentBytes: null,
            rows.Count);
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveCredentialValuesAsync(
        string? credentialSetId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(credentialSetId))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var values = await _credentialResolver.ResolveAsync(credentialSetId, cancellationToken).ConfigureAwait(false);
        return new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<string> RequestAccessTokenAsync(
        HttpClient http,
        BynderManifestBuildOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId) ||
            string.IsNullOrWhiteSpace(options.ClientSecret) ||
            string.IsNullOrWhiteSpace(options.Scopes))
        {
            throw new InvalidOperationException(
                "Bynder manifest builder requires either AccessToken/PermanentToken or ClientId, ClientSecret, and Scopes in the selected credentials.");
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            ["scope"] = options.Scopes
        });

        using var response = await http.PostAsync(options.TokenPath.TrimStart('/'), content, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Bynder token request failed with {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var token = GetString(document.RootElement, "access_token") ??
                    GetString(document.RootElement, "accessToken");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Bynder token response did not include access_token.");
        }

        return token;
    }

    private static async Task<IReadOnlyList<BynderAssetManifestItem>> QueryAssetsAsync(
        HttpClient http,
        BynderManifestBuildOptions options,
        int page,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["limit"] = options.PageSize.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(options.IncludeArchived))
        {
            parameters["archive"] = options.IncludeArchived;
        }

        var query = string.Join(
            "&",
            parameters.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        var url = $"{options.MediaPath.TrimStart('/')}?{query}";

        using var response = await http.GetAsync(url, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Bynder media request failed with {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        return ParseAssets(document.RootElement);
    }

    private static IReadOnlyList<BynderAssetManifestItem> ParseAssets(JsonElement root)
    {
        var assets = new List<BynderAssetManifestItem>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    assets.Add(ParseAsset(item));
                }
            }

            return assets;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return assets;
        }

        if (TryGetProperty(root, "media", out var media) ||
            TryGetProperty(root, "assets", out media) ||
            TryGetProperty(root, "items", out media) ||
            TryGetProperty(root, "data", out media))
        {
            if (media.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in media.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        assets.Add(ParseAsset(item));
                    }
                }

                return assets;
            }

            if (media.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in media.EnumerateObject())
                {
                    if (item.Value.ValueKind == JsonValueKind.Object)
                    {
                        assets.Add(ParseAsset(item.Value));
                    }
                }

                return assets;
            }
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                assets.Add(ParseAsset(property.Value));
            }
        }

        if (assets.Count == 0)
        {
            assets.Add(ParseAsset(root));
        }

        return assets;
    }

    private static BynderAssetManifestItem ParseAsset(JsonElement item)
    {
        var id = GetString(item, "id") ??
                 GetString(item, "mediaId") ??
                 GetString(item, "assetId") ??
                 GetString(item, "databaseId") ??
                 string.Empty;

        var name = GetString(item, "name") ??
                   GetString(item, "title") ??
                   GetString(item, "originalFileName") ??
                   GetString(item, "filename") ??
                   string.Empty;

        var original = GetOriginalUrl(item);
        var thumbnail = GetThumbnailUrl(item);
        var archive = GetString(item, "archiveUrl") ?? GetString(item, "archive_url");
        var type = GetString(item, "type") ?? GetString(item, "extension") ?? GetString(item, "mimeType");
        var created = GetString(item, "dateCreated") ?? GetString(item, "createdAt") ?? GetString(item, "created");
        var modified = GetString(item, "dateModified") ?? GetString(item, "updatedAt") ?? GetString(item, "modified");
        var size = GetLong(item, "fileSize") ?? GetLong(item, "size") ?? GetLong(item, "sizeBytes");

        return new BynderAssetManifestItem(
            id,
            name,
            original ?? string.Empty,
            thumbnail ?? string.Empty,
            archive ?? string.Empty,
            type ?? string.Empty,
            size,
            created ?? string.Empty,
            modified ?? string.Empty,
            item.GetRawText());
    }

    private static string? GetOriginalUrl(JsonElement item)
    {
        if (TryGetProperty(item, "original", out var original))
        {
            if (original.ValueKind == JsonValueKind.String)
            {
                return original.GetString();
            }

            if (original.ValueKind == JsonValueKind.Object)
            {
                return GetString(original, "url") ??
                       GetString(original, "src") ??
                       GetString(original, "downloadUrl");
            }
        }

        if (TryGetProperty(item, "files", out var files) && files.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in files.EnumerateObject())
            {
                if (property.Name.Equals("original", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == JsonValueKind.Object)
                {
                    return GetString(property.Value, "url") ??
                           GetString(property.Value, "src") ??
                           GetString(property.Value, "downloadUrl");
                }
            }
        }

        return GetString(item, "downloadUrl") ??
               GetString(item, "originalUrl") ??
               GetString(item, "url");
    }

    private static string? GetThumbnailUrl(JsonElement item)
    {
        if (TryGetProperty(item, "thumbnails", out var thumbnails))
        {
            if (thumbnails.ValueKind == JsonValueKind.Object)
            {
                return GetString(thumbnails, "webimage") ??
                       GetString(thumbnails, "webImage") ??
                       GetString(thumbnails, "mini") ??
                       GetString(thumbnails, "thumbnail");
            }
        }

        return GetString(item, "thumbnailUrl") ??
               GetString(item, "previewUrl");
    }

    private static string BuildCsv(IReadOnlyList<BynderManifestRow> rows)
    {
        var baseColumns = new List<(string Header, Func<BynderManifestRow, string?> Value)>
        {
            ("RowId", row => row.RowId.ToString(CultureInfo.InvariantCulture)),
            ("SourceType", row => Source),
            ("ServiceName", row => Service),
            ("SourceAssetId", row => row.SourceAssetId),
            ("Name", row => row.Name),
            ("FileName", row => row.FileName),
            ("SourceUri", row => row.SourceUri),
            ("DownloadUrl", row => row.SourceUri),
            ("ThumbnailUrl", row => row.ThumbnailUrl),
            ("ArchiveUrl", row => row.ArchiveUrl),
            ("MimeType", row => row.MimeType),
            ("SizeBytes", row => row.SizeBytes?.ToString(CultureInfo.InvariantCulture)),
            ("Created", row => row.Created),
            ("LastModified", row => row.LastModified)
        };

        var rowProperties = rows
            .Select(row => ParsePropertiesJson(row.PropertiesJson))
            .ToArray();

        var baseColumnNames = new HashSet<string>(
            baseColumns.Select(column => column.Header),
            StringComparer.OrdinalIgnoreCase);

        var propertyColumns = new List<(string JsonName, string Header)>();
        var usedColumnNames = new HashSet<string>(baseColumnNames, StringComparer.OrdinalIgnoreCase);

        foreach (var propertyName in rowProperties
                     .SelectMany(properties => properties.Keys)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
        {
            var header = MakeUniqueColumnName(ToCsvColumnName(propertyName), usedColumnNames);
            usedColumnNames.Add(header);
            propertyColumns.Add((propertyName, header));
        }

        var builder = new StringBuilder();

        var headers = baseColumns
            .Select(column => column.Header)
            .Concat(propertyColumns.Select(column => column.Header));

        builder.AppendLine(string.Join(',', headers.Select(EscapeCsv)));

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var properties = rowProperties[index];

            var values = baseColumns
                .Select(column => column.Value(row))
                .Concat(propertyColumns.Select(column =>
                    properties.TryGetValue(column.JsonName, out var value) ? value : null));

            builder.AppendLine(string.Join(',', values.Select(EscapeCsv)));
        }

        return builder.ToString();
    }

    private static IReadOnlyDictionary<string, string> ParsePropertiesJson(string? propertiesJson)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(propertiesJson))
        {
            return values;
        }

        try
        {
            using var document = JsonDocument.Parse(propertiesJson);

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    values[property.Name] = JsonElementToManifestValue(property.Value);
                }
            }
        }
        catch (JsonException)
        {
            values["Properties"] = propertiesJson;
        }

        return values;
    }

    private static string JsonElementToManifestValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Undefined => string.Empty,
            JsonValueKind.Array => string.Join("|", value.EnumerateArray().Select(JsonElementToManifestValue).Where(item => !string.IsNullOrWhiteSpace(item))),
            JsonValueKind.Object => FlattenJsonObjectValue(value),
            _ => value.GetRawText()
        };
    }

    private static string FlattenJsonObjectValue(JsonElement value)
    {
        var preferred = GetString(value, "name") ??
                        GetString(value, "label") ??
                        GetString(value, "displayName") ??
                        GetString(value, "value") ??
                        GetString(value, "id");

        return !string.IsNullOrWhiteSpace(preferred)
            ? preferred
            : value.GetRawText();
    }

    private static string ToCsvColumnName(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "Property";
        }

        var builder = new StringBuilder();

        foreach (var character in propertyName.Trim())
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        var columnName = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(columnName) ? "Property" : columnName;
    }

    private static string MakeUniqueColumnName(string preferredName, HashSet<string> usedColumnNames)
    {
        if (!usedColumnNames.Contains(preferredName))
        {
            return preferredName;
        }

        var index = 2;

        while (usedColumnNames.Contains($"{preferredName}_{index}"))
        {
            index++;
        }

        return $"{preferredName}_{index}";
    }

    private static bool TryGetProperty(JsonElement item, string name, out JsonElement value)
    {
        if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out value))
        {
            return true;
        }

        if (item.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in item.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string? GetString(JsonElement item, string name)
    {
        if (!TryGetProperty(item, name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static long? GetLong(JsonElement item, string name)
    {
        if (!TryGetProperty(item, name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String &&
            long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        return null;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var requiresQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n');
        var escaped = value.Replace("\"", "\"\"");
        return requiresQuotes ? $"\"{escaped}\"" : escaped;
    }

    private sealed record BynderManifestBuildOptions(
        string BaseUrl,
        string ClientId,
        string ClientSecret,
        string Scopes,
        string? AccessToken,
        string TokenPath,
        string MediaPath,
        int PageSize,
        string? IncludeArchived)
    {
        public static BynderManifestBuildOptions From(
            IReadOnlyDictionary<string, string> requestOptions,
            IReadOnlyDictionary<string, string> credentialValues,
            IConfiguration configuration)
        {
            var section = configuration.GetSection("Bynder");

            var baseUrl =
                GetValue(requestOptions, credentialValues, section, "baseUrl", "BaseUrl", "url", "Url") ??
                throw new InvalidOperationException("Bynder manifest builder requires BaseUrl in the selected credentials or Bynder configuration.");

            return new BynderManifestBuildOptions(
                NormalizeBaseUrl(baseUrl),
                GetValue(requestOptions, credentialValues, section, "clientId", "ClientId") ?? string.Empty,
                GetValue(requestOptions, credentialValues, section, "clientSecret", "ClientSecret") ?? string.Empty,
                GetValue(requestOptions, credentialValues, section, "scopes", "Scopes") ?? "asset:read asset:write meta.assetbank:read",
                GetValue(requestOptions, credentialValues, section, "accessToken", "AccessToken", "permanentToken", "PermanentToken", "token", "Token", "bearerToken", "BearerToken"),
                GetValue(requestOptions, credentialValues, section, "tokenPath", "TokenPath") ?? "v6/authentication/oauth2/token",
                GetValue(requestOptions, credentialValues, section, "mediaPath", "MediaPath") ?? "api/v4/media/",
                GetInt(requestOptions, credentialValues, section, "pageSize", "PageSize") ?? 100,
                GetValue(requestOptions, credentialValues, section, "includeArchived", "IncludeArchived"));
        }

        private static string NormalizeBaseUrl(string value)
        {
            value = value.Trim();

            if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                value = "https://" + value;
            }

            return value.TrimEnd('/');
        }

        private static string? GetValue(
            IReadOnlyDictionary<string, string> requestOptions,
            IReadOnlyDictionary<string, string> credentialValues,
            IConfigurationSection configurationSection,
            params string[] keys)
        {
            foreach (var key in keys)
            {
                var requestValue = GetDictionaryValue(requestOptions, key);
                if (!string.IsNullOrWhiteSpace(requestValue))
                {
                    return requestValue;
                }

                var credentialValue = GetDictionaryValue(credentialValues, key);
                if (!string.IsNullOrWhiteSpace(credentialValue))
                {
                    return credentialValue;
                }

                var configurationValue = configurationSection[key];
                if (!string.IsNullOrWhiteSpace(configurationValue))
                {
                    return configurationValue.Trim();
                }
            }

            return null;
        }

        private static string? GetDictionaryValue(IReadOnlyDictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;
        }

        private static int? GetInt(
            IReadOnlyDictionary<string, string> requestOptions,
            IReadOnlyDictionary<string, string> credentialValues,
            IConfigurationSection configurationSection,
            params string[] keys)
        {
            var value = GetValue(requestOptions, credentialValues, configurationSection, keys);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }
    }

    private sealed record BynderAssetManifestItem(
        string Id,
        string Name,
        string SourceUri,
        string ThumbnailUrl,
        string ArchiveUrl,
        string MimeType,
        long? SizeBytes,
        string Created,
        string LastModified,
        string PropertiesJson);

    private sealed record BynderManifestRow(
        int RowId,
        string SourceAssetId,
        string Name,
        string FileName,
        string SourceUri,
        string ThumbnailUrl,
        string ArchiveUrl,
        string MimeType,
        long? SizeBytes,
        string Created,
        string LastModified,
        string PropertiesJson)
    {
        public static BynderManifestRow From(int rowId, BynderAssetManifestItem asset)
        {
            var fileName = FileNameFromUrl(asset.SourceUri) ??
                           (!string.IsNullOrWhiteSpace(asset.Name) ? asset.Name : asset.Id);

            return new BynderManifestRow(
                rowId,
                asset.Id,
                asset.Name,
                fileName,
                asset.SourceUri,
                asset.ThumbnailUrl,
                asset.ArchiveUrl,
                asset.MimeType,
                asset.SizeBytes,
                asset.Created,
                asset.LastModified,
                asset.PropertiesJson);
        }

        private static string? FileNameFromUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                var fileName = Path.GetFileName(uri.LocalPath);
                return string.IsNullOrWhiteSpace(fileName) ? null : fileName;
            }

            var localFileName = Path.GetFileName(value);
            return string.IsNullOrWhiteSpace(localFileName) ? null : localFileName;
        }
    }
}
