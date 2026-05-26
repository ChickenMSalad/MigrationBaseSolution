using Migration.Application.Models.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IMigrationManifestStore
{
    Task<MigrationManifestRecord?> GetAsync(
        long manifestRecordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MigrationManifestRecord>> GetByRunAsync(
        Guid runId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MigrationManifestRecord record,
        CancellationToken cancellationToken = default);

    Task AddBatchAsync(
        IReadOnlyCollection<MigrationManifestRecord> records,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        long manifestRecordId,
        string status,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default);
}
