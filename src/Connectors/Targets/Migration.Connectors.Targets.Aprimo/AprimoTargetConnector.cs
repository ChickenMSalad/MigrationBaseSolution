
using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Connectors.Targets.Aprimo;

public sealed class AprimoTargetConnector : IAssetTargetConnector
{
    public string Type => "Aprimo";

    public Task<MigrationResult> UpsertAsync(MigrationJobDefinition job, AssetWorkItem item, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MigrationResult
        {
            WorkItemId = item.WorkItemId,
            Success = true,
            TargetAssetId = $"aprimo:{item.WorkItemId}",
            Message = "Base connector resolved. Imported Aprimo/AEM source code is available under Imported/."
        });
    }
}
