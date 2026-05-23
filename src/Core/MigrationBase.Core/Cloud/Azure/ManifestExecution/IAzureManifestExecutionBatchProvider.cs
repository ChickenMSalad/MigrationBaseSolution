using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public interface IAzureManifestExecutionBatchProvider
{
    Task<AzureManifestExecutionBatchResult> GetNextBatchAsync(
        AzureManifestExecutionBatchRequest request,
        CancellationToken cancellationToken);
}
