using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public interface IAzureWorkerDispatchQueueReader
{
    Task<AzureWorkerDispatchReadResult> ReadAsync(
        AzureWorkerDispatchReadRequest request,
        CancellationToken cancellationToken);
}
