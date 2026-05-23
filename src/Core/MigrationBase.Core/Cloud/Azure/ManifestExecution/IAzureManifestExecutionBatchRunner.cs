using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public interface IAzureManifestExecutionBatchRunner
{
    Task<AzureManifestExecutionBatchRunResult> RunBatchAsync(
        AzureManifestExecutionBatchRunRequest request,
        CancellationToken cancellationToken);
}
