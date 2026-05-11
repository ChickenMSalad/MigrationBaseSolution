
using Migration.Application.Abstractions;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.Aem;

public sealed class AemSourceConnector : IAssetSourceConnector
{
    public string Type => "Aem";

    public Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AssetEnvelope
        {
            SourceAssetId = row.SourceAssetId ?? row.RowId,
            Path = row.SourcePath,
            SourceType = ConnectorType.Aem,
            Metadata = new Dictionary<string, string?>(row.Columns)
            {
                ["_adapter"] = "Imported AEM client/source code is available under Imported/Services and Imported/Models."
            }
        });
    }
}
