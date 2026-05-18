using Migration.Application.Models.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IMigrationFailureStore
{
    Task AddAsync(
        MigrationFailureRecord failure,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MigrationFailureRecord>> GetByRunAsync(
        Guid runId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MigrationFailureRecord>> GetByManifestRecordAsync(
        Guid manifestRecordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MigrationFailureRecord>> GetByWorkItemAsync(
        Guid workItemId,
        CancellationToken cancellationToken = default);
}
