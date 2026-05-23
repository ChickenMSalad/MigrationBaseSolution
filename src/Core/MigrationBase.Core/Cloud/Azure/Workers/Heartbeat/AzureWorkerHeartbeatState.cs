namespace MigrationBase.Core.Cloud.Azure.Workers.Heartbeat;

public enum AzureWorkerHeartbeatState
{
    Unknown = 0,
    Starting = 1,
    Healthy = 2,
    Draining = 3,
    Stale = 4,
    Lost = 5,
    Stopped = 6,
    Faulted = 7
}
