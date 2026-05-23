namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public interface IAzureManifestExecutionStatePolicy
{
    bool CanTransition(
        AzureManifestExecutionStatus fromStatus,
        AzureManifestExecutionStatus toStatus);
}
