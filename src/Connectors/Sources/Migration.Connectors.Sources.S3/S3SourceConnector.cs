
using Migration.Application.Abstractions;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.S3;

public sealed class S3SourceConnector : IAssetSourceConnector
{
    public string Type => "S3";

    public Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AssetEnvelope
        {
            SourceAssetId = row.SourceAssetId ?? row.RowId,
            Path = row.SourcePath,
            SourceType = ConnectorType.S3,
            Metadata = new Dictionary<string, string?>(row.Columns)
        });
    }
}
