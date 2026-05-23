namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker;

public interface IAzureWorkerHeartbeatCheckpointRecorder
{
    Task<AzureWorkerHeartbeatCheckpointResult> RecordAsync(
        AzureWorkerRuntimeHeartbeatCheckpoint checkpoint,
        CancellationToken cancellationToken = default);
}
