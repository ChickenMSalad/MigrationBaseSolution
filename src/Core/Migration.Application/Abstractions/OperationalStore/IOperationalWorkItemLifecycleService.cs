using Migration.Application.Models.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalWorkItemLifecycleService
{
    Task<MigrationWorkItemRecord> CreateWorkItemAsync(
        Guid runId,
        long manifestRecordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MigrationWorkItemRecord>> CreateWorkItemBatchAsync(
        Guid runId,
        IReadOnlyCollection<long> manifestRecordIds,
        CancellationToken cancellationToken = default);

    Task MarkWorkItemLockedAsync(
        long workItemId,
        string lockedBy,
        CancellationToken cancellationToken = default);

    Task MarkWorkItemCompletedAsync(
        long workItemId,
        CancellationToken cancellationToken = default);

    Task MarkWorkItemFailedAsync(
        Guid runId,
        long workItemId,
        long manifestRecordId,
        string failureReason,
        bool isRetriable,
        CancellationToken cancellationToken = default);
}
