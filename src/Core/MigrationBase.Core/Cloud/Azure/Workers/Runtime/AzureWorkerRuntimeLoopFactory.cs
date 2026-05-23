namespace MigrationBase.Core.Cloud.Azure.Workers.Runtime;

public static class AzureWorkerRuntimeLoopFactory
{
    public static IAzureWorkerRuntimeLoop Create(AzureWorkerRuntimeLoopOptions options, IEnumerable<IAzureWorkerRuntimeStep>? steps = null)
    {
        return new AzureWorkerRuntimeLoop(options, steps);
    }
}
