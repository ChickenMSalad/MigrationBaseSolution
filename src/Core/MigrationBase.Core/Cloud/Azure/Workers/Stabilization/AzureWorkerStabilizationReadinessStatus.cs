namespace MigrationBase.Core.Cloud.Azure.Workers.Stabilization;

/// <summary>
/// Describes whether a worker stabilization capability is ready for real migration execution.
/// </summary>
public enum AzureWorkerStabilizationReadinessStatus
{
    Unknown = 0,
    NotStarted = 1,
    Defined = 2,
    Validated = 3,
    Blocked = 4
}
