using Migration.Domain.Models;

namespace Migration.Application.Abstractions;

public interface IAssetTargetConnector
{
    string Type { get; }
    Task<MigrationResult> UpsertAsync(MigrationJobDefinition job, AssetWorkItem item, CancellationToken cancellationToken = default);
}
