using Migration.Connectors.Sources.WebDam.Services;
using Migration.ControlPlane.ManifestBuilder;

namespace Migration.Connectors.Sources.WebDam.ManifestBuilder;

public sealed class WebDamExportAllAssetsManifestService : ISourceManifestService
{
    private static readonly string[] Columns =
    [
        "AssetId",
        "FileName",
        "Name",
        "SizeBytes",
        "FileType",
        "FolderId",
        "FolderPath"
    ];

    private readonly WebDamManifestExportServiceFactory _exportServiceFactory;

    public WebDamExportAllAssetsManifestService(WebDamManifestExportServiceFactory exportServiceFactory)
    {
        _exportServiceFactory = exportServiceFactory ?? throw new ArgumentNullException(nameof(exportServiceFactory));
    }

    public string SourceType => "webdam";

    public string ServiceName => "export-all-assets";

    public ManifestBuilderServiceDescriptor GetDescriptor()
    {
        return new ManifestBuilderServiceDescriptor(
            SourceType,
            ServiceName,
            "Export All Assets",
            "Exports all WebDam assets into a migration manifest CSV.",
            [
                new ManifestBuilderOptionDescriptor(
                    "note",
                    "Note",
                    "Optional operator note. This is not sent to WebDam.",
                    Required: false,
                    Placeholder: "Optional note")
            ]);
    }

    public async Task<BuildSourceManifestResult> BuildAsync(
        BuildSourceManifestRequest request,
        CancellationToken cancellationToken = default)
    {
        var exportService = await _exportServiceFactory
            .CreateAsync(request.CredentialSetId, cancellationToken)
            .ConfigureAwait(false);

        var export = await exportService
            .ExportAllAssetsAsync(cancellationToken)
            .ConfigureAwait(false);

        var csv = ManifestCsvWriter.WriteObjects(export.Assets, Columns);
        var fileName = $"webdam-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";

        return new BuildSourceManifestResult(
            SourceType,
            ServiceName,
            fileName,
            "text/csv",
            csv,
            export.Assets.Count);
    }
}