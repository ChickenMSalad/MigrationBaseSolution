namespace MigrationBase.Core.Cloud.Azure.Workers.Diagnostics;

public enum AzureWorkerDiagnosticSnapshotStatus
{
    Unknown = 0,
    Starting = 1,
    Idle = 2,
    Running = 3,
    Draining = 4,
    Stopping = 5,
    Degraded = 6,
    Faulted = 7
}
