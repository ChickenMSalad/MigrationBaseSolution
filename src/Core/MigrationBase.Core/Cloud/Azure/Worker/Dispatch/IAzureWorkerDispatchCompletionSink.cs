using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public interface IAzureWorkerDispatchCompletionSink
{
    Task<AzureWorkerDispatchCompletionResult> CompleteAsync(
        AzureWorkerDispatchCompletionRequest request,
        CancellationToken cancellationToken);
}
