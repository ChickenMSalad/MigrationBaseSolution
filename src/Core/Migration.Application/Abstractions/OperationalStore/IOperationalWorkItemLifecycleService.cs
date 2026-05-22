using Migration.Application.Models.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalWorkItemLifecycleService
{
    Task<MigrationWorkItemRecord> CreateWorkItemAsync(
        Guid runId,
        Guid manifestRecordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MigrationWorkItemRecord>> CreateWorkItemBatchAsync(
        Guid runId,
        IReadOnlyCollection<Guid> manifestRecordIds,
        CancellationToken cancellationToken = default);

    Task MarkWorkItemLockedAsync(
        Guid workItemId,
        string lockedBy,
        CancellationToken cancellationToken = default);

    Task MarkWorkItemCompletedAsync(
        Guid workItemId,
        CancellationToken cancellationToken = default);

    Task MarkWorkItemFailedAsync(
        Guid runId,
        Guid workItemId,
        Guid manifestRecordId,
        string failureReason,
        bool isRetriable,
        CancellationToken cancellationToken = default);
}
