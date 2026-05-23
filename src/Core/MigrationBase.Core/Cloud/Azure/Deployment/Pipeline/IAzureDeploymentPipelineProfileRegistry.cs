namespace MigrationBase.Core.Cloud.Azure.Deployment.Pipeline;

public interface IAzureDeploymentPipelineProfileRegistry
{
    IReadOnlyList<AzureDeploymentPipelineProfile> GetProfiles();

    AzureDeploymentPipelineProfile? FindByName(string name);

    IReadOnlyList<string> Validate();
}
