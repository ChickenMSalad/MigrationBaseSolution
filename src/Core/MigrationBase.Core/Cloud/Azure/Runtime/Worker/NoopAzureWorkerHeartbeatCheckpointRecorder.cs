namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker;

public sealed class NoopAzureWorkerHeartbeatCheckpointRecorder : IAzureWorkerHeartbeatCheckpointRecorder
{
    public Task<AzureWorkerHeartbeatCheckpointResult> RecordAsync(
        AzureWorkerRuntimeHeartbeatCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        var issues = AzureWorkerHeartbeatCheckpointValidator.Validate(checkpoint);
        if (issues.Count > 0)
        {
            return Task.FromResult(AzureWorkerHeartbeatCheckpointResult.Failure(
                "HeartbeatCheckpoint.Invalid",
                string.Join(" ", issues)));
        }

        return Task.FromResult(AzureWorkerHeartbeatCheckpointResult.Success(
            checkpoint.WorkerId,
            checkpoint.ObservedAtUtc));
    }
}
