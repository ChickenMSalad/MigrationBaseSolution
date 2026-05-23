namespace MigrationBase.Core.Cloud.Azure.Deployment.HealthChecks;

public sealed record AzureDeploymentHealthCheckDescriptor(
    string Name,
    string Description,
    AzureDeploymentHealthCheckScope Scope,
    AzureDeploymentHealthCheckSeverity Severity,
    string ExpectedEvidenceKey,
    bool IsRequiredForPromotion);
