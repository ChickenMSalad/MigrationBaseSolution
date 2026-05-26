using Migration.Application.Models.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IMigrationWorkItemStore
{
    Task<MigrationWorkItemRecord?> GetAsync(
        long workItemId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MigrationWorkItemRecord workItem,
        CancellationToken cancellationToken = default);

    Task AddBatchAsync(
        IReadOnlyCollection<MigrationWorkItemRecord> workItems,
        CancellationToken cancellationToken = default);

    Task MarkLockedAsync(
        long workItemId,
        string lockedBy,
        DateTimeOffset lockedAt,
        CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(
        long workItemId,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        long workItemId,
        string failureReason,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default);
}
