namespace MigrationBase.Core.Cloud.Azure.Deployment.HealthChecks;

public interface IAzureDeploymentHealthCheckRegistry
{
    IReadOnlyList<AzureDeploymentHealthCheckDescriptor> GetAll();

    IReadOnlyList<AzureDeploymentHealthCheckDescriptor> GetRequiredForPromotion();
}
