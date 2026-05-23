namespace MigrationBase.Core.Cloud.Azure.Workers.Runtime;

public interface IAzureWorkerRuntimeStep
{
    string Name { get; }

    ValueTask<bool> ExecuteAsync(AzureWorkerRuntimeLoopOptions options, CancellationToken cancellationToken);
}
