namespace MigrationBase.Core.Cloud.Azure.Deployment.Pipeline;

public sealed record AzureDeploymentPipelineStage(
    string Name,
    string EnvironmentName,
    string DeploymentRing,
    bool RequiresApproval,
    bool RequiresReadinessEvidence,
    bool AllowsAutomaticPromotion)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(EnvironmentName)
        && !string.IsNullOrWhiteSpace(DeploymentRing);
}
