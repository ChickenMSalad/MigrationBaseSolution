using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public interface IAzureWorkerDispatchDeferralSink
{
    Task<AzureWorkerDispatchCompletionResult> DeferAsync(
        AzureWorkerDispatchDeferRequest request,
        CancellationToken cancellationToken);
}
