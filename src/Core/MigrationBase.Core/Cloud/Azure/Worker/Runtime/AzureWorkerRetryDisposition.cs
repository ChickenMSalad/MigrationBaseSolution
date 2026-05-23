namespace MigrationBase.Core.Cloud.Azure.Worker.Runtime;

public enum AzureWorkerRetryDisposition
{
    None = 0,
    Retry = 1,
    DoNotRetry = 2,
    MoveToPoison = 3,
    Abandon = 4
}
