using Migration.Domain.Models;

namespace Migration.Application.Abstractions;

public interface IAssetSourceConnector
{
    string Type { get; }
    Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, CancellationToken cancellationToken = default);
}
