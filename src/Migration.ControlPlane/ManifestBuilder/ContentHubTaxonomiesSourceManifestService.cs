using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Migration.ControlPlane.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stylelabs.M.Base.Querying;
using Stylelabs.M.Base.Querying.Linq;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;

namespace Migration.ControlPlane.ManifestBuilder;

public sealed class ContentHubTaxonomiesSourceManifestService : ISourceManifestService
{
    private const string Source = "contenthub";
    private const string Service = "export-taxonomies";

    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialResolver _credentialResolver;
    private readonly ILogger<ContentHubTaxonomiesSourceManifestService> _logger;

    public ContentHubTaxonomiesSourceManifestService(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        ICredentialResolver credentialResolver,
        ILogger<ContentHubTaxonomiesSourceManifestService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
            "Builds a Sitecore Content Hub manifest by exporting assets from one or more taxonomies.",
            new[]
            {
                new ManifestBuilderOptionDescriptor(
                    "taxonomies",
                    "Taxonomies",
                    "One Content Hub taxonomy value per line. Example: M.AssetType\\Products",
                    Required: true,
                    Placeholder: "M.AssetType\\Products"),
                new ManifestBuilderOptionDescriptor(
                    "taxonomyRelation",
                    "Taxonomy relation",
                    "Content Hub relation used to find assets for each taxonomy. Defaults to AssetTypeToAsset.",
                    Required: false,
                    Placeholder: "AssetTypeToAsset"),
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
        var taxonomies = GetTaxonomies(options);

        if (taxonomies.Count == 0)
        {
            throw new ArgumentException("At least one Content Hub taxonomy is required. Enter one taxonomy per line.", nameof(request));
        }

        var taxonomyRelation = GetValue(options, "taxonomyRelation", "relation", "taxonomy.relation");
        if (string.IsNullOrWhiteSpace(taxonomyRelation))
        {
            taxonomyRelation = "AssetTypeToAsset";
        }

        var credentialValues = await ResolveCredentialValuesAsync(request.CredentialSetId, cancellationToken).ConfigureAwait(false);

        var rows = new List<ContentHubManifestRow>();

        var sdkClient = _serviceProvider.GetService<IWebMClient>();
        if (sdkClient is not null)
        {
            rows.AddRange(await BuildWithSdkAsync(
                sdkClient,
                taxonomies,
                taxonomyRelation,
                cancellationToken).ConfigureAwait(false));
        }
        else
        {
            rows.AddRange(await BuildWithHttpFallbackAsync(
                credentialValues,
                options,
                taxonomies,
                taxonomyRelation,
                cancellationToken).ConfigureAwait(false));
        }

        var csv = BuildCsv(rows);
        var fileName = $"contenthub-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";

        return new BuildSourceManifestResult(
            Source,
            Service,
            fileName,
            "text/csv",
            csv,
            ContentBytes: null,
            rows.Count);
    }

    private async Task<IReadOnlyList<ContentHubManifestRow>> BuildWithSdkAsync(
        IWebMClient client,
        IReadOnlyList<string> taxonomies,
        string taxonomyRelation,
        CancellationToken cancellationToken)
    {
        var rows = new List<ContentHubManifestRow>();
        var rowId = 1;

        foreach (var taxonomy in taxonomies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "Building Content Hub manifest rows through IWebMClient for taxonomy {Taxonomy} using relation {Relation}.",
                taxonomy,
                taxonomyRelation);

            var taxonomyEntity = await client.Entities.GetAsync(taxonomy).ConfigureAwait(false);

            if (taxonomyEntity?.Id is null)
            {
                _logger.LogWarning("Content Hub taxonomy {Taxonomy} was not found.", taxonomy);
                continue;
            }

            var taxonomyId = taxonomyEntity.Id.Value;
            var relation = taxonomyRelation;

            var query = Query.CreateQuery(entities =>
                from entity in entities
                where entity.DefinitionName == "M.Asset" &&
                      entity.Parent(relation) == taxonomyId
                select entity);

            var queryResult = await client.Querying.QueryIdsAsync(query).ConfigureAwait(false);

            foreach (var id in queryResult.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                rows.Add(new ContentHubManifestRow(
                    rowId++,
                    taxonomy,
                    id.ToString() ?? string.Empty,
                    "M.Asset",
                    string.Empty,
                    string.Empty,
                    string.Empty));
            }
        }

        return rows;
    }

    private async Task<IReadOnlyList<ContentHubManifestRow>> BuildWithHttpFallbackAsync(
        IReadOnlyDictionary<string, string> credentialValues,
        IReadOnlyDictionary<string, string> options,
        IReadOnlyList<string> taxonomies,
        string taxonomyRelation,
        CancellationToken cancellationToken)
    {
        var baseUrl = GetValue(options, "baseUrl", "BaseUrl")
            ?? GetValue(credentialValues, "BaseUrl", "baseUrl", "ContentHubBaseUrl", "Url", "url");

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "No Sitecore Content Hub IWebMClient is registered and no BaseUrl was supplied. Select a ContentHub credential set with BaseUrl or enter baseUrl in options.");
        }

        var http = _httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(EnsureTrailingSlash(baseUrl));

        ApplyAuthorization(http, credentialValues);

        var rows = new List<ContentHubManifestRow>();
        var rowId = 1;

        foreach (var taxonomy in taxonomies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "Building Content Hub manifest rows through HTTP fallback for taxonomy {Taxonomy} using relation {Relation}.",
                taxonomy,
                taxonomyRelation);

            var payload = await QueryFakeContentHubAsync(http, taxonomy, taxonomyRelation, cancellationToken).ConfigureAwait(false);

            foreach (var row in payload)
            {
                rows.Add(row with
                {
                    RowId = rowId++,
                    Taxonomy = string.IsNullOrWhiteSpace(row.Taxonomy) ? taxonomy : row.Taxonomy
                });
            }
        }

        return rows;
    }

    private static async Task<IReadOnlyList<ContentHubManifestRow>> QueryFakeContentHubAsync(
        HttpClient http,
        string taxonomy,
        string taxonomyRelation,
        CancellationToken cancellationToken)
    {
        var query = $"taxonomy={Uri.EscapeDataString(taxonomy)}&taxonomyRelation={Uri.EscapeDataString(taxonomyRelation)}";

        var candidates = new[]
        {
            $"api/manifest-builder/contenthub/assets?{query}",
            $"api/contenthub/assets?{query}",
            $"contenthub/assets?{query}",
            $"assets?{query}"
        };

        HttpResponseMessage? lastResponse = null;

        foreach (var candidate in candidates)
        {
            var response = await http.GetAsync(candidate, cancellationToken).ConfigureAwait(false);
            lastResponse = response;

            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            return ParseRows(document.RootElement, taxonomy);
        }

        var status = lastResponse is null
            ? "no response"
            : $"{(int)lastResponse.StatusCode} {lastResponse.ReasonPhrase}";

        throw new InvalidOperationException(
            $"Unable to query fake Content Hub at {http.BaseAddress}. Tried known manifest endpoints and received {status}.");
    }

    private static IReadOnlyList<ContentHubManifestRow> ParseRows(JsonElement root, string taxonomy)
    {
        var rows = new List<ContentHubManifestRow>();

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (TryGetProperty(root, "items", out var items) ||
                TryGetProperty(root, "assets", out items) ||
                TryGetProperty(root, "data", out items) ||
                TryGetProperty(root, "results", out items))
            {
                AddArrayRows(rows, items, taxonomy);
                return rows;
            }

            rows.Add(ParseRow(root, taxonomy));
            return rows;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            AddArrayRows(rows, root, taxonomy);
        }

        return rows;
    }

    private static void AddArrayRows(List<ContentHubManifestRow> rows, JsonElement array, string taxonomy)
    {
        if (array.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                rows.Add(ParseRow(item, taxonomy));
            }
            else if (item.ValueKind == JsonValueKind.String)
            {
                rows.Add(new ContentHubManifestRow(
                    0,
                    taxonomy,
                    item.GetString() ?? string.Empty,
                    "M.Asset",
                    string.Empty,
                    string.Empty,
                    string.Empty));
            }
        }
    }

    private static ContentHubManifestRow ParseRow(JsonElement item, string taxonomy)
    {
        var id = GetJsonValue(item, "id", "Id", "assetId", "AssetId", "sourceAssetId", "SourceAssetId");
        var definitionName = GetJsonValue(item, "definitionName", "DefinitionName") ?? "M.Asset";
        var fileName = GetJsonValue(item, "fileName", "FileName", "filename", "Filename", "name", "Name");
        var title = GetJsonValue(item, "title", "Title", "assetTitle", "AssetTitle");
        var rowTaxonomy = GetJsonValue(item, "taxonomy", "Taxonomy") ?? taxonomy;

        return new ContentHubManifestRow(
            0,
            rowTaxonomy,
            id ?? string.Empty,
            definitionName,
            fileName ?? string.Empty,
            title ?? string.Empty,
            item.GetRawText());
    }

    private static IReadOnlyList<string> GetTaxonomies(IReadOnlyDictionary<string, string> options)
    {
        var raw = GetValue(
            options,
            "taxonomies",
            "taxonomy",
            "taxonomyList",
            "exportTaxonomies",
            "export.taxonomies");

        return (raw ?? string.Empty)
            .Split(new[] { '\r', '\n', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

    private static void ApplyAuthorization(HttpClient http, IReadOnlyDictionary<string, string> credentialValues)
    {
        var token = GetValue(credentialValues, "AccessToken", "BearerToken", "Token", "ApiToken");

        if (!string.IsNullOrWhiteSpace(token))
        {
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static string? GetJsonValue(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(item, name, out var property))
            {
                return property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString(),
                    JsonValueKind.Number => property.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => property.GetRawText()
                };
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

    private static string EnsureTrailingSlash(string value)
    {
        value = value.Trim();
        return value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
    }

    private static string BuildCsv(IReadOnlyList<ContentHubManifestRow> rows)
    {
        var builder = new StringBuilder();

        builder.AppendLine(string.Join(',', new[]
        {
            "RowId",
            "SourceType",
            "ServiceName",
            "Taxonomy",
            "SourceAssetId",
            "DefinitionName",
            "FileName",
            "Title",
            "Properties"
        }.Select(EscapeCsv)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', new[]
            {
                row.RowId.ToString(CultureInfo.InvariantCulture),
                Source,
                Service,
                row.Taxonomy,
                row.SourceAssetId,
                row.DefinitionName,
                row.FileName,
                row.Title,
                row.Properties
            }.Select(EscapeCsv)));
        }

        return builder.ToString();
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

    private sealed record ContentHubManifestRow(
        int RowId,
        string Taxonomy,
        string SourceAssetId,
        string? DefinitionName,
        string? FileName,
        string? Title,
        string Properties);
}
