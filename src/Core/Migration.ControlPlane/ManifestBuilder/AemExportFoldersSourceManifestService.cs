using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Migration.ControlPlane.Services;

namespace Migration.ControlPlane.ManifestBuilder;

public sealed class AemExportFoldersSourceManifestService : ISourceManifestService
{
    private const string Source = "AEM";
    private const string Service = "ExportFolders";

    private readonly IConfiguration _configuration;
    private readonly ICredentialResolver _credentialResolver;
    private readonly IHttpClientFactory _httpClientFactory;

    public AemExportFoldersSourceManifestService(
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
            "AEM folder export manifest",
            "Builds an AEM manifest from one or more DAM folders.",
            new[]
            {
                new ManifestBuilderOptionDescriptor(
                    "folders",
                    "Export folders",
                    "One AEM DAM folder path per line. Example: /content/dam/site/folder",
                    Required: true,
                    Placeholder: "/content/dam/site/folder-one\n/content/dam/site/folder-two"),
                new ManifestBuilderOptionDescriptor(
                    "recursive",
                    "Recursive",
                    "true/false. Include assets below each selected folder.",
                    Required: false,
                    Placeholder: "true")
            });
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
        var options = AemManifestBuildOptions.From(requestOptions, credentialValues, _configuration);

        if (options.Folders.Count == 0)
        {
            throw new InvalidOperationException("At least one AEM export folder is required.");
        }

        using var http = CreateClient(options);
        var rows = new List<AemManifestRow>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in options.Folders)
        {
            var assets = await QueryAssetsAsync(http, options, folder, cancellationToken).ConfigureAwait(false);

            foreach (var asset in assets)
            {
                if (!seenPaths.Add(asset.Path))
                {
                    continue;
                }

                rows.Add(AemManifestRow.From(rows.Count + 1, folder, asset));
            }
        }

        var csv = BuildCsv(rows);
        var fileName = $"aem-folder-export-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";

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

    private HttpClient CreateClient(AemManifestBuildOptions options)
    {
        var http = _httpClientFactory.CreateClient();
        http.Timeout = TimeSpan.FromMinutes(10);
        http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

        if (options.AuthType.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.TokenOrUser))
            {
                throw new InvalidOperationException("AEM bearer authentication requires DeveloperTokenOrUser, TokenOrUser, token, bearerToken, or accessToken in the selected credentials.");
            }

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.TokenOrUser);

            if (!string.IsNullOrWhiteSpace(options.ClientId))
            {
                http.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", options.ClientId);
            }

            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        }
        else if (options.AuthType.Equals("Basic", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.TokenOrUser) || string.IsNullOrWhiteSpace(options.Password))
            {
                throw new InvalidOperationException("AEM basic authentication requires DeveloperTokenOrUser/TokenOrUser/userName and Password in the selected credentials.");
            }

            var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.TokenOrUser}:{options.Password}"));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", raw);
        }

        return http;
    }

    private static async Task<IReadOnlyList<AemAssetManifestItem>> QueryAssetsAsync(
        HttpClient http,
        AemManifestBuildOptions options,
        string folder,
        CancellationToken cancellationToken)
    {
        var assets = new List<AemAssetManifestItem>();
        var offset = 0;
        var total = int.MaxValue;

        while (offset < total)
        {
            var queryUrl = BuildQueryBuilderUrl(options, folder, offset);
            using var response = await http.GetAsync(queryUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var root = document.RootElement;
            total = TryGetInt(root, "total") ?? assets.Count;

            if (!root.TryGetProperty("hits", out var hits) || hits.ValueKind != JsonValueKind.Array || hits.GetArrayLength() == 0)
            {
                break;
            }

            foreach (var hit in hits.EnumerateArray())
            {
                var path = GetString(hit, "path") ?? GetString(hit, "jcr:path");

                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                assets.Add(new AemAssetManifestItem(
                    GetString(hit, "jcr:uuid") ?? GetString(hit, "uuid") ?? string.Empty,
                    GetTitle(hit) ?? Path.GetFileName(path),
                    path,
                    GetMetadataString(hit, "dam:MIMEtype") ?? GetMetadataString(hit, "dc:format") ?? string.Empty,
                    GetMetadataLong(hit, "dam:size"),
                    GetString(hit, "jcr:created") ?? string.Empty,
                    GetNestedString(hit, "jcr:content", "jcr:lastModified") ?? string.Empty));
            }

            offset += options.PageSize;
        }

        return assets;
    }

    private static string BuildQueryBuilderUrl(AemManifestBuildOptions options, string folder, int offset)
    {
        var queryRoot = options.QueryBuilderRoot.TrimStart('/');
        var parameters = new Dictionary<string, string>
        {
            ["path"] = folder,
            ["type"] = "dam:Asset",
            ["mainasset"] = "true",
            ["p.limit"] = options.PageSize.ToString(CultureInfo.InvariantCulture),
            ["p.offset"] = offset.ToString(CultureInfo.InvariantCulture),
            ["p.hits"] = "selective",
            ["p.properties"] = "jcr:path jcr:uuid jcr:created jcr:content/jcr:lastModified jcr:content/metadata/dam:size jcr:content/metadata/dam:MIMEtype jcr:content/metadata/dc:title"
        };

        if (!options.Recursive)
        {
            parameters["path.flat"] = "true";
        }

        var query = string.Join(
            "&",
            parameters.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        return $"{queryRoot}?{query}";
    }

    private static string BuildCsv(IReadOnlyList<AemManifestRow> rows)
    {
        var columns = new List<(string Header, Func<AemManifestRow, string?> Value)>
        {
            ("RowId", row => row.RowId.ToString(CultureInfo.InvariantCulture)),
            ("SourceType", row => Source),
            ("ServiceName", row => Service),
            ("SourceFolder", row => row.SourceFolder),
            ("SourceAssetId", row => row.SourceAssetId),
            ("Path", row => row.Path),
            ("Name", row => row.Name),
            ("FileName", row => row.FileName),
            ("FolderPath", row => row.FolderPath),
            ("MimeType", row => row.MimeType),
            ("SizeBytes", row => row.SizeBytes?.ToString(CultureInfo.InvariantCulture)),
            ("Created", row => row.Created),
            ("LastModified", row => row.LastModified)
        };

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', columns.Select(column => EscapeCsv(column.Header))));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', columns.Select(column => EscapeCsv(column.Value(row)))));
        }

        return builder.ToString();
    }

    private static string? GetTitle(JsonElement hit)
    {
        return GetMetadataString(hit, "dc:title")
            ?? GetMetadataString(hit, "jcr:title")
            ?? GetString(hit, "dc:title");
    }

    private static string? GetMetadataString(JsonElement hit, string name)
    {
        if (hit.TryGetProperty("jcr:content", out var content) &&
            content.ValueKind == JsonValueKind.Object &&
            content.TryGetProperty("metadata", out var metadata) &&
            metadata.ValueKind == JsonValueKind.Object)
        {
            return GetString(metadata, name);
        }

        return null;
    }

    private static long? GetMetadataLong(JsonElement hit, string name)
    {
        if (hit.TryGetProperty("jcr:content", out var content) &&
            content.ValueKind == JsonValueKind.Object &&
            content.TryGetProperty("metadata", out var metadata) &&
            metadata.ValueKind == JsonValueKind.Object)
        {
            return TryGetLong(metadata, name);
        }

        return null;
    }

    private static string? GetNestedString(JsonElement element, string container, string name)
    {
        return element.TryGetProperty(container, out var nested) && nested.ValueKind == JsonValueKind.Object
            ? GetString(nested, name)
            : null;
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
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

    private static int? TryGetInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        return null;
    }

    private static long? TryGetLong(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
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

    private sealed record AemManifestBuildOptions(
        string BaseUrl,
        string ClientId,
        string AuthType,
        string TokenOrUser,
        string? Password,
        string QueryBuilderRoot,
        int PageSize,
        bool Recursive,
        IReadOnlyList<string> Folders)
    {
        public static AemManifestBuildOptions From(
            IReadOnlyDictionary<string, string> requestOptions,
            IReadOnlyDictionary<string, string> credentialValues,
            IConfiguration configuration)
        {
            var section = configuration.GetSection("AemSource");
            var baseUrl = GetValue(requestOptions, credentialValues, section, "baseUrl", "BaseUrl", "url", "host")
                ?? throw new InvalidOperationException("AEM manifest builder requires BaseUrl in the selected credentials or AemSource configuration.");

            return new AemManifestBuildOptions(
                NormalizeBaseUrl(baseUrl),
                GetValue(requestOptions, credentialValues, section, "clientId", "ClientId", "apiKey", "xApiKey") ?? string.Empty,
                GetValue(requestOptions, credentialValues, section, "authType", "AuthType") ?? "Bearer",
                GetValue(requestOptions, credentialValues, section, "developerTokenOrUser", "DeveloperTokenOrUser", "tokenOrUser", "TokenOrUser", "token", "bearerToken", "accessToken", "userName", "username") ?? string.Empty,
                GetValue(requestOptions, credentialValues, section, "password", "Password"),
                GetValue(requestOptions, credentialValues, section, "queryBuilderRoot", "QueryBuilderRoot") ?? "bin/querybuilder.json",
                GetInt(requestOptions, credentialValues, section, "pageSize", "PageSize") ?? 500,
                GetBool(requestOptions, "recursive", defaultValue: true),
                GetFolders(requestOptions));
        }

        private static IReadOnlyList<string> GetFolders(IReadOnlyDictionary<string, string> requestOptions)
        {
            var raw = GetDictionaryValue(requestOptions, "folders")
                ?? GetDictionaryValue(requestOptions, "folder")
                ?? GetDictionaryValue(requestOptions, "exportFolders")
                ?? string.Empty;

            return raw
                .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(folder => folder.Trim())
                .Where(folder => !string.IsNullOrWhiteSpace(folder))
                .Select(folder => folder.StartsWith("/", StringComparison.Ordinal) ? folder : "/" + folder)
                .Select(folder => folder.TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
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

        private static bool GetBool(IReadOnlyDictionary<string, string> options, string key, bool defaultValue)
        {
            var value = GetDictionaryValue(options, key);
            return bool.TryParse(value, out var result) ? result : defaultValue;
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

    private sealed record AemAssetManifestItem(
        string Id,
        string Name,
        string Path,
        string MimeType,
        long? SizeBytes,
        string Created,
        string LastModified);

    private sealed record AemManifestRow(
        int RowId,
        string SourceFolder,
        string SourceAssetId,
        string Path,
        string Name,
        string FileName,
        string FolderPath,
        string MimeType,
        long? SizeBytes,
        string Created,
        string LastModified)
    {
        public static AemManifestRow From(int rowId, string sourceFolder, AemAssetManifestItem asset)
        {
            var normalizedPath = asset.Path.Replace('\\', '/');
            var fileName = System.IO.Path.GetFileName(normalizedPath);
            var folderPath = normalizedPath.Contains('/', StringComparison.Ordinal)
                ? normalizedPath[..normalizedPath.LastIndexOf('/')]
                : string.Empty;

            return new AemManifestRow(
                rowId,
                sourceFolder,
                string.IsNullOrWhiteSpace(asset.Id) ? normalizedPath : asset.Id,
                normalizedPath,
                asset.Name,
                fileName,
                folderPath,
                asset.MimeType,
                asset.SizeBytes,
                asset.Created,
                asset.LastModified);
        }
    }
}
