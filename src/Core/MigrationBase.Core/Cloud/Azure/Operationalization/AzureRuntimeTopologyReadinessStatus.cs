namespace MigrationBase.Core.Cloud.Azure.Operationalization;

/// <summary>
/// Describes the readiness state for an Azure runtime topology check.
/// </summary>
public enum AzureRuntimeTopologyReadinessStatus
{
    Unknown = 0,
    NotStarted = 1,
    InProgress = 2,
    Ready = 3,
    Blocked = 4,
    Deferred = 5
}
