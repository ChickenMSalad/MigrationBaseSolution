namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public interface IAzureManifestExecutionContextFactory
{
    AzureManifestExecutionContext Create(AzureManifestExecutionContextRequest request);
}
