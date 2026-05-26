using Migration.Infrastructure.Sql.Records;

namespace Migration.Infrastructure.Sql.Stores;

public interface ISqlOperationalBackboneStore
{
    Task CreateProjectAsync(SqlMigrationProjectRecord project, CancellationToken cancellationToken = default);

    Task CreateRunAsync(SqlMigrationRunRecord run, CancellationToken cancellationToken = default);

    Task<SqlMigrationRunRecord?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);

    Task UpsertManifestRowsAsync(
        IReadOnlyCollection<SqlMigrationManifestRowRecord> rows,
        CancellationToken cancellationToken = default);

    Task EnqueueWorkItemsAsync(
        IReadOnlyCollection<SqlMigrationWorkItemRecord> workItems,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SqlMigrationWorkItemRecord>> LeaseWorkItemsAsync(
        Guid runId,
        string leaseOwner,
        int maxItems,
        CancellationToken cancellationToken = default);

    Task CompleteWorkItemAsync(long workItemId, CancellationToken cancellationToken = default);

    Task FailWorkItemAsync(
        long workItemId,
        SqlMigrationFailureRecord failure,
        CancellationToken cancellationToken = default);

    Task SaveCheckpointAsync(SqlMigrationCheckpointRecord checkpoint, CancellationToken cancellationToken = default);

    Task UpsertAssetMappingAsync(SqlMigrationAssetMappingRecord mapping, CancellationToken cancellationToken = default);
}
