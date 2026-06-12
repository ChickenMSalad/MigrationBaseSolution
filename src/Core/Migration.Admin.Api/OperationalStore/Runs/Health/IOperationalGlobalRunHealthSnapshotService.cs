namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalRunHealthSnapshotService
{
    Task<OperationalGlobalRunHealthSnapshotResponse> GetSnapshotAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default);
}


