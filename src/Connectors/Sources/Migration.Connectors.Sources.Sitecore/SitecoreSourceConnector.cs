
using Migration.Application.Abstractions;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.Sitecore;

public sealed class SitecoreSourceConnector : IAssetSourceConnector
{
    public string Type => "Sitecore";

    public Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AssetEnvelope
        {
            SourceAssetId = row.SourceAssetId ?? row.RowId,
            Path = row.SourcePath,
            SourceType = ConnectorType.Sitecore,
            Metadata = new Dictionary<string, string?>(row.Columns)
            {
                ["_adapter"] = "Imported Content Hub / Sitecore-adjacent source code is available under Imported/ContentHub and Imported/Node."
            }
        });
    }
}
