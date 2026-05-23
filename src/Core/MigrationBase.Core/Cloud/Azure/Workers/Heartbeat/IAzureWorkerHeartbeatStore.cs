namespace MigrationBase.Core.Cloud.Azure.Workers.Heartbeat;

public interface IAzureWorkerHeartbeatStore
{
    Task RecordHeartbeatAsync(AzureWorkerHeartbeatDescriptor heartbeat, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AzureWorkerHeartbeatDescriptor>> ListActiveHeartbeatsAsync(
        string environmentName,
        CancellationToken cancellationToken = default);
}
