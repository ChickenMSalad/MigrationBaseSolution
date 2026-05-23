namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public enum AzureWorkerDispatcherReadinessStatus
{
    Ready = 0,
    Degraded = 1,
    NotReady = 2
}
