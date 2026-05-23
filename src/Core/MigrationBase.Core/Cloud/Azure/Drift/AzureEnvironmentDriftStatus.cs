namespace MigrationBase.Core.Cloud.Azure.Drift;

public enum AzureEnvironmentDriftStatus
{
    Unknown = 0,
    InSync = 1,
    DriftDetected = 2,
    ValidationFailed = 3
}
