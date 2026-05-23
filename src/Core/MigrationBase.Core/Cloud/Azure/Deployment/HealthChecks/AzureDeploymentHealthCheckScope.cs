namespace MigrationBase.Core.Cloud.Azure.Deployment.HealthChecks;

public enum AzureDeploymentHealthCheckScope
{
    Unknown = 0,
    Infrastructure = 1,
    Application = 2,
    Worker = 3,
    SqlOperationalStore = 4,
    Storage = 5,
    Queue = 6,
    Telemetry = 7,
    Security = 8
}
