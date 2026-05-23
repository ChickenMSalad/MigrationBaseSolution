namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public interface IAzureManifestExecutionPlanBuilder
{
    AzureManifestExecutionPlan Build(AzureManifestExecutionPlanRequest request);
}
