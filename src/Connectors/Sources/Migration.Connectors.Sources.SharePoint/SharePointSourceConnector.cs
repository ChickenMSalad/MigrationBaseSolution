
using Migration.Application.Abstractions;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.SharePoint;

public sealed class SharePointSourceConnector : IAssetSourceConnector
{
    public string Type => "SharePoint";

    public Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AssetEnvelope
        {
            SourceAssetId = row.SourceAssetId ?? row.RowId,
            Path = row.SourcePath,
            SourceType = ConnectorType.SharePoint,
            Metadata = new Dictionary<string, string?>(row.Columns)
        });
    }
}
