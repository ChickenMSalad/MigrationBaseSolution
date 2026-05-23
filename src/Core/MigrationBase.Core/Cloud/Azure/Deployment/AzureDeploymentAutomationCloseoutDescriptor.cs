namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Describes the closeout state for Azure deployment automation before moving into observability.
/// </summary>
public sealed class AzureDeploymentAutomationCloseoutDescriptor
{
    public string EnvironmentName { get; init; } = string.Empty;
    public string DeploymentRing { get; init; } = string.Empty;
    public string ReleaseArtifactId { get; init; } = string.Empty;
    public string PipelineRunId { get; init; } = string.Empty;
    public string ReadinessEvidenceId { get; init; } = string.Empty;
    public string HealthCheckEvidenceId { get; init; } = string.Empty;
    public bool InfrastructureValidated { get; init; }
    public bool ApplicationValidated { get; init; }
    public bool RollbackPlanValidated { get; init; }
    public bool PromotionGatesSatisfied { get; init; }
    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
