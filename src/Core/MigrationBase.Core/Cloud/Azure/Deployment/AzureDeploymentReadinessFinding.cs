namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Captures the result of evaluating one Azure deployment readiness check.
/// </summary>
public sealed record AzureDeploymentReadinessFinding(
    string CheckId,
    string Name,
    string Category,
    AzureDeploymentReadinessSeverity Severity,
    bool Passed,
    string Message,
    string RecommendedAction)
{
    public bool BlocksDeployment => !Passed && Severity >= AzureDeploymentReadinessSeverity.Blocking;
}
