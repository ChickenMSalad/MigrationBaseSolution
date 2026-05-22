using Migration.Application.Models.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IMigrationWorkItemStore
{
    Task<MigrationWorkItemRecord?> GetAsync(
        Guid workItemId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MigrationWorkItemRecord workItem,
        CancellationToken cancellationToken = default);

    Task AddBatchAsync(
        IReadOnlyCollection<MigrationWorkItemRecord> workItems,
        CancellationToken cancellationToken = default);

    Task MarkLockedAsync(
        Guid workItemId,
        string lockedBy,
        DateTimeOffset lockedAt,
        CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(
        Guid workItemId,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid workItemId,
        string failureReason,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default);
}
