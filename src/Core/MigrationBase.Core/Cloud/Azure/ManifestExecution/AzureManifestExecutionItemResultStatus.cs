namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public enum AzureManifestExecutionItemResultStatus
{
    Succeeded = 0,
    Skipped = 1,
    Failed = 2,
    Deferred = 3
}
