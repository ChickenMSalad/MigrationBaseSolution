using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public interface IAzureWorkerDispatchCoordinator
{
    Task<AzureWorkerDispatchClaimBatchResult> ReadAndClaimAsync(
        AzureWorkerDispatchClaimBatchRequest request,
        CancellationToken cancellationToken);
}
