using Migration.Connectors.Sources.WebDam.Services;
using Migration.ControlPlane.ManifestBuilder;

namespace Migration.Connectors.Sources.WebDam.ManifestBuilder;

public sealed class WebDamExportAllAssetsManifestService : ISourceManifestService
{
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
            "Exports all WebDam assets into a migration manifest file.",
            [
                new ManifestBuilderOptionDescriptor(
                    "format",
                    "Format",
                    "Use csv for a single migration-ready manifest, or xlsx for a legacy-style workbook with Assets, Metadata, and Metadata Schema sheets.",
                    Required: false,
                    Placeholder: "csv or xlsx")
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

        var format = request.Options is not null &&
                     request.Options.TryGetValue("format", out var requestedFormat) &&
                     !string.IsNullOrWhiteSpace(requestedFormat)
            ? requestedFormat.Trim().ToLowerInvariant()
            : "csv";

        return format switch
        {
            "xlsx" or "excel" => new BuildSourceManifestResult(
                SourceType,
                ServiceName,
                $"webdam-export-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Content: null,
                ContentBytes: WebDamExportFileWriter.WriteWorkbook(export),
                export.Assets.Count),

            "csv" => new BuildSourceManifestResult(
                SourceType,
                ServiceName,
                $"webdam-manifest-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv",
                "text/csv",
                Content: null,
                ContentBytes: WebDamExportFileWriter.WriteManifestCsv(export),
                export.Assets.Count),

            _ => throw new ArgumentException("Unsupported WebDam export format. Use 'csv' or 'xlsx'.")
        };
    }
}