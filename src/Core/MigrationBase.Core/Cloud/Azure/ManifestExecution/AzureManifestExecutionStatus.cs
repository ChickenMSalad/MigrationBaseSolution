namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public enum AzureManifestExecutionStatus
{
    NotStarted = 0,
    Preparing = 1,
    Running = 2,
    Paused = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}
