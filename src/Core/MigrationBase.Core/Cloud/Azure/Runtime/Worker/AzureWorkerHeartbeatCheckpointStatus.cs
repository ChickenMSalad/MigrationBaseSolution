namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker;

public enum AzureWorkerHeartbeatCheckpointStatus
{
    Unknown = 0,
    Starting = 1,
    Running = 2,
    Draining = 3,
    Stopping = 4,
    Stopped = 5,
    Faulted = 6
}
