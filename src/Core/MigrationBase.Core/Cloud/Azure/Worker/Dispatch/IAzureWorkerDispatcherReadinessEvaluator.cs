using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public interface IAzureWorkerDispatcherReadinessEvaluator
{
    Task<AzureWorkerDispatcherReadinessReport> EvaluateAsync(
        AzureWorkerDispatcherReadinessRequest request,
        CancellationToken cancellationToken);
}
