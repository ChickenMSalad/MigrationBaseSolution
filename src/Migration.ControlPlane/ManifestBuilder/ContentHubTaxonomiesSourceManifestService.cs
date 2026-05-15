using System.Globalization;
using System.Text;
using Migration.ControlPlane.ManifestBuilder;
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
    private readonly ILogger<ContentHubTaxonomiesSourceManifestService> _logger;

    public ContentHubTaxonomiesSourceManifestService(
        IServiceProvider serviceProvider,
        ILogger<ContentHubTaxonomiesSourceManifestService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
                    Placeholder: "AssetTypeToAsset")
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

        var client = _serviceProvider.GetService<IWebMClient>();
        if (client is null)
        {
            throw new InvalidOperationException("No Sitecore Content Hub IWebMClient is registered. Verify the Sitecore/Content Hub source connector registration is included in the Admin API runtime.");
        }

        var rows = new List<ContentHubManifestRow>();
        var rowId = 1;

        foreach (var taxonomy in taxonomies)
        {
            _logger.LogInformation(
                "Building Content Hub manifest rows for taxonomy {Taxonomy} using relation {Relation}.",
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

            var scroller = client.Querying.CreateEntityScroller(query, TimeSpan.FromMinutes(5));

            while (await scroller.MoveNextAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var entity in scroller.Current.Items)
                {
                    rows.Add(ContentHubManifestRow.From(rowId++, taxonomy, entity));
                }
            }
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
        string Properties)
    {
        public static ContentHubManifestRow From(int rowId, string taxonomy, IEntity entity)
        {
            var sourceAssetId = entity.Id?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            var fileName = TryGetPropertyValue(entity, "FileName")
                ?? TryGetPropertyValue(entity, "Filename")
                ?? TryGetPropertyValue(entity, "fileName")
                ?? string.Empty;

            var title = TryGetPropertyValue(entity, "Title")
                ?? TryGetPropertyValue(entity, "AssetTitle")
                ?? TryGetPropertyValue(entity, "Name")
                ?? string.Empty;

            var properties = string.Join(
                "|",
                entity.Properties
                    .Select(property => $"{property.Name}={TryGetPropertyValue(entity, property.Name)}")
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

            return new ContentHubManifestRow(
                rowId,
                taxonomy,
                sourceAssetId,
                entity.DefinitionName,
                fileName,
                title,
                properties);
        }

        private static string? TryGetPropertyValue(IEntity entity, string propertyName)
        {
            try
            {
                var value = entity.GetPropertyValue(propertyName);
                return value?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
