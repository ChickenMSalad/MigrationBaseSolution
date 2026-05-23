namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionStatePolicy : IAzureManifestExecutionStatePolicy
{
    public bool CanTransition(
        AzureManifestExecutionStatus fromStatus,
        AzureManifestExecutionStatus toStatus)
    {
        if (fromStatus == toStatus)
        {
            return true;
        }

        return fromStatus switch
        {
            AzureManifestExecutionStatus.NotStarted =>
                toStatus is AzureManifestExecutionStatus.Preparing or AzureManifestExecutionStatus.Cancelled,

            AzureManifestExecutionStatus.Preparing =>
                toStatus is AzureManifestExecutionStatus.Running or AzureManifestExecutionStatus.Failed or AzureManifestExecutionStatus.Cancelled,

            AzureManifestExecutionStatus.Running =>
                toStatus is AzureManifestExecutionStatus.Paused or AzureManifestExecutionStatus.Completed or AzureManifestExecutionStatus.Failed or AzureManifestExecutionStatus.Cancelled,

            AzureManifestExecutionStatus.Paused =>
                toStatus is AzureManifestExecutionStatus.Running or AzureManifestExecutionStatus.Cancelled,

            AzureManifestExecutionStatus.Completed =>
                false,

            AzureManifestExecutionStatus.Failed =>
                false,

            AzureManifestExecutionStatus.Cancelled =>
                false,

            _ => false
        };
    }
}
