namespace MigrationBase.Core.Cloud.Azure.Deployment.HealthChecks;

public enum AzureDeploymentHealthCheckStatus
{
    Unknown = 0,
    Passed = 1,
    Warning = 2,
    Failed = 3,
    NotApplicable = 4
}
