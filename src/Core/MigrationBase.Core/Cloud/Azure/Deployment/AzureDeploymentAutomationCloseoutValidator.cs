namespace MigrationBase.Core.Cloud.Azure.Deployment;

public static class AzureDeploymentAutomationCloseoutValidator
{
    public static AzureDeploymentAutomationCloseoutResult Validate(AzureDeploymentAutomationCloseoutDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var result = new AzureDeploymentAutomationCloseoutResult();

        Require(descriptor.EnvironmentName, nameof(descriptor.EnvironmentName), result);
        Require(descriptor.DeploymentRing, nameof(descriptor.DeploymentRing), result);
        Require(descriptor.ReleaseArtifactId, nameof(descriptor.ReleaseArtifactId), result);
        Require(descriptor.PipelineRunId, nameof(descriptor.PipelineRunId), result);
        Require(descriptor.ReadinessEvidenceId, nameof(descriptor.ReadinessEvidenceId), result);
        Require(descriptor.HealthCheckEvidenceId, nameof(descriptor.HealthCheckEvidenceId), result);

        if (!descriptor.InfrastructureValidated)
        {
            result.Errors.Add("Infrastructure validation must be completed before deployment automation closeout.");
        }

        if (!descriptor.ApplicationValidated)
        {
            result.Errors.Add("Application validation must be completed before deployment automation closeout.");
        }

        if (!descriptor.RollbackPlanValidated)
        {
            result.Errors.Add("Rollback plan validation must be completed before deployment automation closeout.");
        }

        if (!descriptor.PromotionGatesSatisfied)
        {
            result.Errors.Add("Promotion gates must be satisfied before deployment automation closeout.");
        }

        return result;
    }

    private static void Require(string value, string name, AzureDeploymentAutomationCloseoutResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result.Errors.Add($"{name} is required.");
        }
    }
}
