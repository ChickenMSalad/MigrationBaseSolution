namespace MigrationBase.Core.Cloud.Azure.Observability;

public enum AzureHealthSignalStatus
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3,
    Offline = 4
}
