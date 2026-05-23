using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public interface IAzureManifestExecutionCheckpointStore
{
    Task<AzureManifestExecutionCheckpointResult> RecordAsync(
        AzureManifestExecutionCheckpointRequest request,
        CancellationToken cancellationToken);

    Task<AzureManifestExecutionCheckpoint?> GetLatestAsync(
        string executionId,
        CancellationToken cancellationToken);
}
