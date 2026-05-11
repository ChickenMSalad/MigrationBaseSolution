using Migration.Domain.Models;

namespace Migration.Connectors.Sources.SharePoint.ManifestBuilder;

public interface ISharePointManifestBuilder
{
    Task<IReadOnlyList<ManifestRow>> BuildAsync(MigrationJobDefinition job, CancellationToken cancellationToken = default);
    Task WriteCsvAsync(MigrationJobDefinition job, string outputPath, CancellationToken cancellationToken = default);
}
