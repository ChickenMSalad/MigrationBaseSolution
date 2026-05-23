using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public interface IAzureWorkerDispatchDeadLetterSink
{
    Task<AzureWorkerDispatchDeadLetterResult> DeadLetterAsync(
        AzureWorkerDispatchDeadLetterRequest request,
        CancellationToken cancellationToken);
}
