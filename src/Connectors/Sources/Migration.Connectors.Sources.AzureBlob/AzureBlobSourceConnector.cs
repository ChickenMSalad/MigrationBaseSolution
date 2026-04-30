
using Migration.Application.Abstractions;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.AzureBlob;

public sealed class AzureBlobSourceConnector : IAssetSourceConnector
{
    public string Type => "AzureBlob";

    public Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AssetEnvelope
        {
            SourceAssetId = row.SourceAssetId ?? row.RowId,
            Path = row.SourcePath,
            SourceType = ConnectorType.AzureBlob,
            Metadata = new Dictionary<string, string?>(row.Columns)
        });
    }
}
