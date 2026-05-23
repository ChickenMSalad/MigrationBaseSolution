namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Severity assigned to an Azure deployment readiness finding.
/// </summary>
public enum AzureDeploymentReadinessSeverity
{
    Informational = 0,
    Warning = 1,
    Error = 2,
    Blocking = 3
}
