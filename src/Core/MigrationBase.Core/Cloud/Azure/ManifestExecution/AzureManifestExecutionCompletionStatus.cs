namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public enum AzureManifestExecutionCompletionStatus
{
    Completed = 0,
    CompletedWithWarnings = 1,
    Failed = 2,
    Cancelled = 3
}
