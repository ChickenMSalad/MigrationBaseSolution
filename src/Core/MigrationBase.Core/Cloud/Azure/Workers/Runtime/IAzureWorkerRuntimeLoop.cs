namespace MigrationBase.Core.Cloud.Azure.Workers.Runtime;

public interface IAzureWorkerRuntimeLoop
{
    ValueTask<AzureWorkerRuntimeLoopResult> RunOnceAsync(CancellationToken cancellationToken = default);
}
