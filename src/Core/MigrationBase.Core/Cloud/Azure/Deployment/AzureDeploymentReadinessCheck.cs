namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Describes a single Azure deployment readiness check that can be evaluated by deployment tooling.
/// </summary>
public sealed record AzureDeploymentReadinessCheck(
    string CheckId,
    string Name,
    string Category,
    AzureDeploymentReadinessSeverity Severity,
    string Description,
    string RecommendedAction,
    bool RequiredForProduction);
