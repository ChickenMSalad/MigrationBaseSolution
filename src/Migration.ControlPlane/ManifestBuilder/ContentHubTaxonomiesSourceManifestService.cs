using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Migration.ControlPlane.Services;
using Microsoft.Extensions.Logging;

namespace Migration.ControlPlane.ManifestBuilder;

public sealed class ContentHubTaxonomiesSourceManifestService : ISourceManifestService
{
    private const string Source = "contenthub";
    private const string Service = "export-taxonomies";
    private const int PageSize = 500;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialResolver _credentialResolver;
    private readonly ILogger<ContentHubTaxonomiesSourceManifestService> _logger;

    public ContentHubTaxonomiesSourceManifestService(
        IHttpClientFactory httpClientFactory,
        ICredentialResolver credentialResolver,
        ILogger<ContentHubTaxonomiesSourceManifestService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _credentialResolver = credentialResolver ?? throw new ArgumentNullException(nameof(credentialResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string SourceType => Source;

    public string ServiceName => Service;

    public ManifestBuilderServiceDescriptor GetDescriptor()
    {
        return new ManifestBuilderServiceDescriptor(
            Source,
            Service,
            "Export taxonomies",
            "Builds a Sitecore Content Hub manifest. Use * or all to export every asset from fake Content Hub.",
            new[]
            {
                new ManifestBuilderOptionDescriptor(
                    "taxonomies",
                    "Taxonomies",
                    "One Content Hub taxonomy value per line. Use * or all to export all assets.",
                    Required: true,
                    Placeholder: "*"),
                new ManifestBuilderOptionDescriptor(
                    "taxonomyRelation",
                    "Taxonomy relation",
                    "Content Hub relation used to find assets for each taxonomy. Defaults to AssetTypeToAsset.",
                    Required: false,
                    Placeholder: "AssetTypeToAsset"),
                new ManifestBuilderOptionDescriptor(
                    "endpointPath",
                    "Endpoint path",
                    "Optional override for fake Content Hub asset endpoint.",
                    Required: false,
                    Placeholder: "/api/entities"),
                new ManifestBuilderOptionDescriptor(
                    "baseUrl",
                    "Base URL",
                    "Optional Content Hub/fake Content Hub base URL. Credential BaseUrl is used when selected.",
                    Required: false,
                    Placeholder: "http://98.81.167.252:8095/")
            });
    }

    public async Task<BuildSourceManifestResult> BuildAsync(
        BuildSourceManifestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = request.Options ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var credentialValues = await ResolveCredentialValuesAsync(request.CredentialSetId, cancellationToken).ConfigureAwait(false);

        var baseUrl = GetValue(options, "baseUrl", "BaseUrl")
            ?? GetValue(credentialValues, "BaseUrl", "baseUrl", "ContentHubBaseUrl", "Url", "url");

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "Content Hub BaseUrl is required. Select a ContentHub/Sitecore credential set with BaseUrl or enter baseUrl in options.");
        }

        var taxonomies = GetTaxonomies(options);

        if (taxonomies.Count == 0)
        {
            throw new ArgumentException("At least one Content Hub taxonomy is required. Enter one taxonomy per line, or use * to export all assets.", nameof(request));
        }

        var taxonomyRelation = GetValue(options, "taxonomyRelation", "relation", "taxonomy.relation");
        if (string.IsNullOrWhiteSpace(taxonomyRelation))
        {
            taxonomyRelation = "AssetTypeToAsset";
        }

        var http = _httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(EnsureTrailingSlash(baseUrl));

        await ApplyAuthorizationAsync(http, credentialValues, cancellationToken).ConfigureAwait(false);

        var endpointMap = await DiscoverEndpointsAsync(http, cancellationToken).ConfigureAwait(false);
        var endpointOverride = GetValue(options, "endpointPath", "EndpointPath", "assetEndpoint", "AssetEndpoint");

        var allRows = new List<ContentHubManifestRow>();
        var seenAssetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var attempted = new List<string>();
        var rowId = 1;

        foreach (var taxonomy in taxonomies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var exportAll = IsAllAssetsTaxonomy(taxonomy);

            var rows = await QueryContentHubAssetsAsync(
                http,
                taxonomy,
                taxonomyRelation,
                exportAll,
                endpointOverride,
                endpointMap,
                attempted,
                cancellationToken).ConfigureAwait(false);

            foreach (var row in rows)
            {
                var identity = string.IsNullOrWhiteSpace(row.SourceAssetId)
                    ? row.PropertiesJsonSignature
                    : row.SourceAssetId;

                if (!seenAssetIds.Add(identity))
                {
                    continue;
                }

                allRows.Add(row with { RowId = rowId++ });
            }
        }

        if (allRows.Count == 0)
        {
            throw new InvalidOperationException(
                "Content Hub manifest returned 0 asset rows. Tried endpoints: " +
                string.Join(", ", attempted.Distinct(StringComparer.OrdinalIgnoreCase)) +
                ". Try Taxonomies '*' and leave Endpoint path blank; or set Endpoint path to the exact fake ContentHub asset endpoint.");
        }

        var csv = BuildCsv(allRows);
        var fileName = $"contenthub-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";

        return new BuildSourceManifestResult(
            Source,
            Service,
            fileName,
            "text/csv",
            csv,
            ContentBytes: null,
            allRows.Count);
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveCredentialValuesAsync(
        string? credentialSetId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(credentialSetId))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return await _credentialResolver.ResolveAsync(credentialSetId, cancellationToken).ConfigureAwait(false);
    }

    private async Task ApplyAuthorizationAsync(
        HttpClient http,
        IReadOnlyDictionary<string, string> credentialValues,
        CancellationToken cancellationToken)
    {
        var token = GetValue(
            credentialValues,
            "AccessToken",
            "accessToken",
            "BearerToken",
            "bearerToken",
            "Token",
            "token",
            "ApiToken",
            "apiToken");

        if (string.IsNullOrWhiteSpace(token))
        {
            token = await RequestTokenAsync(http, credentialValues, cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private async Task<string?> RequestTokenAsync(
        HttpClient http,
        IReadOnlyDictionary<string, string> credentialValues,
        CancellationToken cancellationToken)
    {
        var clientId = GetValue(credentialValues, "ClientId", "clientId", "client_id");
        var clientSecret = GetValue(credentialValues, "ClientSecret", "clientSecret", "client_secret");
        var username = GetValue(credentialValues, "Username", "UserName", "username", "userName");
        var password = GetValue(credentialValues, "Password", "password");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogInformation("Content Hub ClientId/ClientSecret were not supplied; skipping token request.");
            return null;
        }

        var tokenEndpoint = GetValue(credentialValues, "TokenUrl", "tokenUrl", "TokenEndpoint", "tokenEndpoint")
            ?? "oauth/token";

        tokenEndpoint = NormalizeEndpoint(tokenEndpoint);

        var attempts = new List<TokenRequestAttempt>
        {
            // Fake ContentHub accepts this shape.
            new(
                "json client credentials",
                new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        client_id = clientId,
                        client_secret = clientSecret
                    }),
                    Encoding.UTF8,
                    "application/json")),

            // Some token handlers expect form-url-encoded client credentials.
            new(
                "form client credentials",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret
                }))
        };

        if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
        {
            var form = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                form["username"] = username;
            }

            if (!string.IsNullOrWhiteSpace(password))
            {
                form["password"] = password;
            }

            attempts.Add(new TokenRequestAttempt("form client credentials with username/password", new FormUrlEncodedContent(form)));
        }

        var failures = new List<string>();

        foreach (var attempt in attempts)
        {
            using var content = attempt.Content;

            using var response = await http.PostAsync(
                tokenEndpoint,
                content,
                cancellationToken).ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                failures.Add($"{attempt.Name}: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
                continue;
            }

            using var document = JsonDocument.Parse(body);

            var token = GetJsonValue(
                document.RootElement,
                "access_token",
                "accessToken",
                "token",
                "bearerToken",
                "BearerToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            failures.Add($"{attempt.Name}: successful response did not contain access_token/accessToken/token. Response: {body}");
        }

        throw new InvalidOperationException(
            $"Unable to obtain Content Hub access token from {tokenEndpoint}. Attempts: {string.Join(" | ", failures)}");
    }

    private async Task<IReadOnlyDictionary<string, string>> DiscoverEndpointsAsync(
        HttpClient http,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync(string.Empty, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!TryGetProperty(document.RootElement, "endpoints", out var endpoints) ||
                endpoints.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return endpoints
                .EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => JsonElementToManifestValue(property.Value) ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to discover fake Content Hub endpoints from base URL.");
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task<IReadOnlyList<ContentHubManifestRow>> QueryContentHubAssetsAsync(
        HttpClient http,
        string taxonomy,
        string taxonomyRelation,
        bool exportAll,
        string? endpointOverride,
        IReadOnlyDictionary<string, string> endpointMap,
        IList<string> attempted,
        CancellationToken cancellationToken)
    {
        var endpointCandidates = BuildEndpointCandidates(taxonomy, taxonomyRelation, exportAll, endpointOverride, endpointMap);

        foreach (var endpoint in endpointCandidates)
        {
            var rows = await TryEndpointAsync(http, endpoint, taxonomy, exportAll, attempted, cancellationToken).ConfigureAwait(false);

            if (rows.Count > 0)
            {
                return rows;
            }
        }

        return Array.Empty<ContentHubManifestRow>();
    }

    private async Task<IReadOnlyList<ContentHubManifestRow>> TryEndpointAsync(
        HttpClient http,
        EndpointCandidate endpoint,
        string taxonomy,
        bool exportAll,
        IList<string> attempted,
        CancellationToken cancellationToken)
    {
        var rows = new List<ContentHubManifestRow>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var maxPages = endpoint.SupportsPaging ? 1000 : 1;

        for (var page = 1; page <= maxPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = endpoint.Build(page);
            attempted.Add(endpoint.Method + " " + path);

            using var response = endpoint.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)
                ? await http.PostAsync(path, BuildPostContent(page), cancellationToken).ConfigureAwait(false)
                : await http.GetAsync(path, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                break;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var pageRows = ParseRows(document.RootElement, taxonomy, exportAll);

            if (pageRows.Count == 0)
            {
                break;
            }

            var added = 0;

            foreach (var row in pageRows)
            {
                var identity = string.IsNullOrWhiteSpace(row.SourceAssetId)
                    ? row.PropertiesJsonSignature
                    : row.SourceAssetId;

                if (seen.Add(identity))
                {
                    rows.Add(row);
                    added++;
                }
            }

            if (!endpoint.SupportsPaging || added == 0 || pageRows.Count < PageSize)
            {
                break;
            }
        }

        if (rows.Count > 0)
        {
            _logger.LogInformation(
                "Content Hub endpoint {Method} {Endpoint} returned {Count} manifest rows.",
                endpoint.Method,
                endpoint.Template,
                rows.Count);
        }

        return rows;
    }

    private static StringContent BuildPostContent(int page)
    {
        var payload = new
        {
            definitionName = "M.Asset",
            definition = "M.Asset",
            page,
            take = PageSize,
            skip = (page - 1) * PageSize
        };

        return new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    }

    private static IReadOnlyList<EndpointCandidate> BuildEndpointCandidates(
        string taxonomy,
        string taxonomyRelation,
        bool exportAll,
        string? endpointOverride,
        IReadOnlyDictionary<string, string> endpointMap)
    {
        var candidates = new List<EndpointCandidate>();

        if (!string.IsNullOrWhiteSpace(endpointOverride))
        {
            candidates.Add(new EndpointCandidate("GET", NormalizeEndpoint(endpointOverride), SupportsPaging(endpointOverride)));
        }

        AddMappedEndpoint(candidates, endpointMap, "entities");
        AddMappedEndpoint(candidates, endpointMap, "query", method: "POST");
        AddMappedEndpoint(candidates, endpointMap, "queryIds", method: "POST");
        AddMappedEndpoint(candidates, endpointMap, "scroller", method: "POST");

        if (exportAll)
        {
            candidates.Add(new EndpointCandidate("GET", "api/entities", false));
            candidates.Add(new EndpointCandidate("GET", $"api/entities?page={{page}}&take={PageSize}", true));
            candidates.Add(new EndpointCandidate("GET", $"api/entities?skip={{skip}}&take={PageSize}", true));
            candidates.Add(new EndpointCandidate("POST", "api/entities/query", true));
            candidates.Add(new EndpointCandidate("POST", "api/entities/queryids", true));
            candidates.Add(new EndpointCandidate("POST", "api/entities/scroller", true));
        }
        else
        {
            var encodedTaxonomy = Uri.EscapeDataString(taxonomy);
            var encodedRelation = Uri.EscapeDataString(taxonomyRelation);

            candidates.Add(new EndpointCandidate("GET", $"api/entities?taxonomy={encodedTaxonomy}&taxonomyRelation={encodedRelation}", false));
            candidates.Add(new EndpointCandidate("GET", $"api/entities?taxonomy={encodedTaxonomy}&taxonomyRelation={encodedRelation}&page={{page}}&take={PageSize}", true));
            candidates.Add(new EndpointCandidate("POST", "api/entities/query", true));
            candidates.Add(new EndpointCandidate("POST", "api/entities/queryids", true));
            candidates.Add(new EndpointCandidate("POST", "api/entities/scroller", true));
        }

        return candidates
            .GroupBy(candidate => candidate.Method + " " + candidate.Template, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private static void AddMappedEndpoint(
        IList<EndpointCandidate> candidates,
        IReadOnlyDictionary<string, string> endpointMap,
        string key,
        string method = "GET")
    {
        if (!endpointMap.TryGetValue(key, out var endpoint) || string.IsNullOrWhiteSpace(endpoint))
        {
            return;
        }

        var normalized = NormalizeEndpoint(endpoint);
        candidates.Add(new EndpointCandidate(method, normalized, false));
        candidates.Add(new EndpointCandidate(method, $"{normalized}?page={{page}}&take={PageSize}", true));
        candidates.Add(new EndpointCandidate(method, $"{normalized}?skip={{skip}}&take={PageSize}", true));
    }

    private static IReadOnlyList<ContentHubManifestRow> ParseRows(JsonElement root, string taxonomy, bool exportAll)
    {
        var rows = new List<ContentHubManifestRow>();

        if (IsStatusDocument(root))
        {
            return rows;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            AddArrayRows(rows, root, taxonomy, exportAll);
            return rows;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return rows;
        }

        if (TryGetProperty(root, "items", out var items) ||
            TryGetProperty(root, "entities", out items) ||
            TryGetProperty(root, "assets", out items) ||
            TryGetProperty(root, "data", out items) ||
            TryGetProperty(root, "results", out items) ||
            TryGetProperty(root, "members", out items))
        {
            AddArrayRows(rows, items, taxonomy, exportAll);
            return rows;
        }

        var objectValues = root
            .EnumerateObject()
            .Where(property => property.Value.ValueKind == JsonValueKind.Object && !IsStatusDocument(property.Value))
            .Select(property => property.Value)
            .ToArray();

        if (objectValues.Length > 0)
        {
            foreach (var value in objectValues)
            {
                rows.Add(ParseRow(value, taxonomy, exportAll));
            }

            return rows;
        }

        if (exportAll)
        {
            rows.Add(ParseRow(root, taxonomy, exportAll));
        }

        return rows;
    }

    private static bool IsStatusDocument(JsonElement root)
    {
        return root.ValueKind == JsonValueKind.Object &&
               TryGetProperty(root, "service", out var service) &&
               (JsonElementToManifestValue(service)?.Contains("contenthub", StringComparison.OrdinalIgnoreCase) == true) &&
               TryGetProperty(root, "status", out _);
    }

    private static void AddArrayRows(
        List<ContentHubManifestRow> rows,
        JsonElement array,
        string taxonomy,
        bool exportAll)
    {
        if (array.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object && !IsStatusDocument(item))
            {
                rows.Add(ParseRow(item, taxonomy, exportAll));
            }
            else if (item.ValueKind == JsonValueKind.String)
            {
                rows.Add(new ContentHubManifestRow(
                    0,
                    exportAll ? "All" : taxonomy,
                    item.GetString() ?? string.Empty,
                    "M.Asset",
                    string.Empty,
                    string.Empty,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    item.GetString() ?? string.Empty));
            }
        }
    }

    private static ContentHubManifestRow ParseRow(JsonElement item, string taxonomy, bool exportAll)
    {
        var id = GetJsonValue(item, "id", "Id", "identifier", "Identifier", "sourceAssetId", "SourceAssetId", "assetId", "AssetId");
        var definitionName = GetJsonValue(item, "definitionName", "DefinitionName", "definition", "Definition") ?? "M.Asset";
        var fileName = GetJsonValue(item, "fileName", "FileName", "filename", "Filename", "name", "Name");
        var title = GetJsonValue(item, "title", "Title", "assetTitle", "AssetTitle", "name", "Name");
        var rowTaxonomy = exportAll
            ? GetJsonValue(item, "taxonomy", "Taxonomy", "assetType", "AssetType") ?? "All"
            : GetJsonValue(item, "taxonomy", "Taxonomy") ?? taxonomy;

        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        FlattenAssetProperties(item, properties, prefix: null);

        return new ContentHubManifestRow(
            0,
            rowTaxonomy,
            id ?? string.Empty,
            definitionName,
            fileName ?? string.Empty,
            title ?? string.Empty,
            properties,
            item.GetRawText());
    }

    private static void FlattenAssetProperties(
        JsonElement item,
        IDictionary<string, string> properties,
        string? prefix)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in item.EnumerateObject())
        {
            if (string.IsNullOrWhiteSpace(prefix) && IsStandardField(property.Name))
            {
                continue;
            }

            var propertyName = string.IsNullOrWhiteSpace(prefix)
                ? property.Name
                : $"{prefix}_{property.Name}";

            FlattenValue(property.Value, properties, propertyName);
        }
    }

    private static void FlattenValue(JsonElement value, IDictionary<string, string> properties, string propertyName)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                if (TryGetPreferredObjectValue(value, out var preferred))
                {
                    properties[propertyName] = preferred;
                    return;
                }

                FlattenAssetProperties(value, properties, propertyName);
                break;

            case JsonValueKind.Array:
                properties[propertyName] = string.Join("|",
                    value.EnumerateArray()
                        .Select(JsonElementToManifestValue)
                        .Where(text => !string.IsNullOrWhiteSpace(text))
                        .Distinct(StringComparer.OrdinalIgnoreCase));
                break;

            default:
                properties[propertyName] = JsonElementToManifestValue(value) ?? string.Empty;
                break;
        }
    }

    private static bool TryGetPreferredObjectValue(JsonElement value, out string text)
    {
        text = GetJsonValue(value, "name", "label", "displayName", "value", "id") ?? string.Empty;
        return !string.IsNullOrWhiteSpace(text);
    }

    private static string? JsonElementToManifestValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            return TryGetPreferredObjectValue(value, out var preferred) ? preferred : value.GetRawText();
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            JsonValueKind.Array => string.Join("|", value.EnumerateArray().Select(JsonElementToManifestValue).Where(text => !string.IsNullOrWhiteSpace(text))),
            _ => value.GetRawText()
        };
    }

    private static string BuildCsv(IReadOnlyList<ContentHubManifestRow> rows)
    {
        var baseHeaders = new[]
        {
            "RowId",
            "SourceType",
            "ServiceName",
            "Taxonomy",
            "SourceAssetId",
            "DefinitionName",
            "FileName",
            "Title"
        };

        var propertyColumns = rows
            .SelectMany(row => row.Properties.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var builder = new StringBuilder();

        builder.AppendLine(string.Join(',', baseHeaders.Concat(propertyColumns.Select(ToSafeColumnName)).Select(EscapeCsv)));

        foreach (var row in rows)
        {
            var values = new List<string?>
            {
                row.RowId.ToString(CultureInfo.InvariantCulture),
                Source,
                Service,
                row.Taxonomy,
                row.SourceAssetId,
                row.DefinitionName,
                row.FileName,
                row.Title
            };

            foreach (var propertyColumn in propertyColumns)
            {
                values.Add(row.Properties.TryGetValue(propertyColumn, out var value) ? value : string.Empty);
            }

            builder.AppendLine(string.Join(',', values.Select(EscapeCsv)));
        }

        return builder.ToString();
    }

    private static IReadOnlyList<string> GetTaxonomies(IReadOnlyDictionary<string, string> options)
    {
        var raw = GetValue(options, "taxonomies", "taxonomy", "taxonomyList", "exportTaxonomies", "export.taxonomies");

        return (raw ?? string.Empty)
            .Split(new[] { '\r', '\n', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? GetJsonValue(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(item, name, out var property))
            {
                return JsonElementToManifestValue(property);
            }
        }

        return null;
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

    private static string? GetValue(IReadOnlyDictionary<string, string> options, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static bool IsAllAssetsTaxonomy(string value)
    {
        return value.Equals("*", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("all", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("all assets", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("M.Asset", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        endpoint = endpoint.Trim();

        if (endpoint.StartsWith("/", StringComparison.Ordinal))
        {
            endpoint = endpoint[1..];
        }

        return endpoint;
    }

    private static bool SupportsPaging(string endpoint)
    {
        return endpoint.Contains("{page}", StringComparison.OrdinalIgnoreCase) ||
               endpoint.Contains("{skip}", StringComparison.OrdinalIgnoreCase) ||
               endpoint.Contains("page=", StringComparison.OrdinalIgnoreCase) ||
               endpoint.Contains("take=", StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSlash(string value)
    {
        value = value.Trim();
        return value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
    }

    private static string ToSafeColumnName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Property";
        }

        var builder = new StringBuilder();

        foreach (var ch in value.Trim())
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }

        var text = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(text) ? "Property" : text;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n')
            ? $"\"{escaped}\""
            : escaped;
    }

    private static bool IsStandardField(string name)
    {
        return name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("identifier", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("sourceAssetId", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("definitionName", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("definition", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("fileName", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("filename", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("name", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("title", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("assetTitle", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("taxonomy", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record TokenRequestAttempt(string Name, HttpContent Content);

    private sealed record EndpointCandidate(string Method, string Template, bool SupportsPaging)
    {
        public string Build(int page)
        {
            var value = Template
                .Replace("{page}", page.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{skip}", ((page - 1) * PageSize).ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

            return NormalizeEndpoint(value);
        }
    }

    private sealed record ContentHubManifestRow(
        int RowId,
        string Taxonomy,
        string SourceAssetId,
        string? DefinitionName,
        string? FileName,
        string? Title,
        IReadOnlyDictionary<string, string> Properties,
        string PropertiesJsonSignature);
}
