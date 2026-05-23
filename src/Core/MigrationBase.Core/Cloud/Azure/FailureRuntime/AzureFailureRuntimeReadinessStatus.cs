namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public enum AzureFailureRuntimeReadinessStatus
{
    Ready = 0,
    Degraded = 1,
    NotReady = 2
}
