namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public enum AzureWorkerDispatchCompletionStatus
{
    Completed = 0,
    Failed = 1,
    Abandoned = 2,
    Deferred = 3,
    Poisoned = 4
}
