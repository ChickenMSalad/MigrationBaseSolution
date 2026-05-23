namespace MigrationBase.Core.Cloud.Azure.Topology;

/// <summary>
/// Describes whether the P5.1 Azure runtime topology baseline is ready to hand off
/// into the P5.2 worker stabilization phase.
/// </summary>
public enum AzureRuntimeTopologyHandoffStatus
{
    Unknown = 0,
    NotStarted = 1,
    InProgress = 2,
    ReadyForWorkerStabilization = 3,
    Blocked = 4
}
