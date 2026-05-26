using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Migration.Application.Models.OperationalStore.Statuses;

namespace Migration.Application.OperationalStore;

public sealed class OperationalWorkItemLifecycleService : IOperationalWorkItemLifecycleService
{
    private readonly IOperationalStore _operationalStore;

    public OperationalWorkItemLifecycleService(
        IOperationalStore operationalStore)
    {
        _operationalStore = operationalStore;
    }

    public async Task<MigrationWorkItemRecord> CreateWorkItemAsync(
        Guid runId,
        long manifestRecordId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var workItem = new MigrationWorkItemRecord
        {
            // WorkItemId intentionally not assigned; SQL identity owns it.
            RunId = runId,
            ManifestRecordId = manifestRecordId,
            Status = MigrationWorkItemStatuses.Created,
            AttemptCount = 0,
            CreatedAt = now
        };

        await _operationalStore.WorkItems.AddAsync(
            workItem,
            cancellationToken);

        return workItem;
    }

    public async Task<IReadOnlyList<MigrationWorkItemRecord>> CreateWorkItemBatchAsync(
        Guid runId,
        IReadOnlyCollection<long> manifestRecordIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifestRecordIds);

        if (manifestRecordIds.Count == 0)
        {
            return Array.Empty<MigrationWorkItemRecord>();
        }

        var now = DateTimeOffset.UtcNow;

        var workItems = manifestRecordIds
            .Select(manifestRecordId => new MigrationWorkItemRecord
            {
                // WorkItemId intentionally not assigned; SQL identity owns it.
                RunId = runId,
                ManifestRecordId = manifestRecordId,
                Status = MigrationWorkItemStatuses.Created,
                AttemptCount = 0,
                CreatedAt = now
            })
            .ToArray();

        await _operationalStore.WorkItems.AddBatchAsync(
            workItems,
            cancellationToken);

        return workItems;
    }

    public Task MarkWorkItemLockedAsync(
        long workItemId,
        string lockedBy,
        CancellationToken cancellationToken = default)
    {
        return _operationalStore.WorkItems.MarkLockedAsync(
            workItemId,
            lockedBy,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public Task MarkWorkItemCompletedAsync(
        long workItemId,
        CancellationToken cancellationToken = default)
    {
        return _operationalStore.WorkItems.MarkCompletedAsync(
            workItemId,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public async Task MarkWorkItemFailedAsync(
        Guid runId,
        long workItemId,
        long manifestRecordId,
        string failureReason,
        bool isRetriable,
        CancellationToken cancellationToken = default)
    {
        var failedAt = DateTimeOffset.UtcNow;

        await _operationalStore.WorkItems.MarkFailedAsync(
            workItemId,
            failureReason,
            failedAt,
            cancellationToken);

        await _operationalStore.Failures.AddAsync(
            new MigrationFailureRecord
            {
                FailureId = Guid.NewGuid(),
                RunId = runId,
                ManifestRecordId = manifestRecordId,
                WorkItemId = workItemId,
                FailureType = MigrationFailureTypes.WorkItemFailure,
                Message = failureReason,
                IsRetriable = isRetriable,
                CreatedAt = failedAt
            },
            cancellationToken);
    }
}
