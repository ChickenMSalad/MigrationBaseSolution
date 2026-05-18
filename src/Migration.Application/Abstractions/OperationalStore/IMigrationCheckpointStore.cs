using Migration.Application.Models.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IMigrationCheckpointStore
{
    Task<MigrationCheckpointRecord?> GetAsync(
        Guid runId,
        string checkpointName,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        MigrationCheckpointRecord checkpoint,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MigrationCheckpointRecord>> GetByRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
}
