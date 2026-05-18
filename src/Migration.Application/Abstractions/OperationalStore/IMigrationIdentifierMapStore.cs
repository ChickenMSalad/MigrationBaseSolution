using Migration.Application.Models.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IMigrationIdentifierMapStore
{
    Task<MigrationIdentifierMapRecord?> GetBySourceIdAsync(
        Guid runId,
        string sourceId,
        CancellationToken cancellationToken = default);

    Task<MigrationIdentifierMapRecord?> GetByManifestRecordIdAsync(
        Guid manifestRecordId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MigrationIdentifierMapRecord record,
        CancellationToken cancellationToken = default);

    Task AddBatchAsync(
        IReadOnlyCollection<MigrationIdentifierMapRecord> records,
        CancellationToken cancellationToken = default);
}
