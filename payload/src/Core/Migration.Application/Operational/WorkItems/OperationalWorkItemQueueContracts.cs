namespace Migration.Application.Operational.WorkItems;

public interface IOperationalWorkItemQueue
{
    Task<OperationalWorkItemRecord> EnqueueAsync(EnqueueOperationalWorkItemRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalWorkItemRecord>> ClaimAsync(ClaimOperationalWorkItemsRequest request, CancellationToken cancellationToken = default);

    Task<OperationalWorkItemRecord?> GetAsync(Guid workItemId, CancellationToken cancellationToken = default);

    Task<OperationalWorkItemRunSummary> GetRunSummaryAsync(Guid runId, CancellationToken cancellationToken = default);

    Task CompleteAsync(CompleteOperationalWorkItemRequest request, CancellationToken cancellationToken = default);

    Task FailAsync(FailOperationalWorkItemRequest request, CancellationToken cancellationToken = default);

    Task ReleaseAsync(ReleaseOperationalWorkItemRequest request, CancellationToken cancellationToken = default);
}

public sealed record EnqueueOperationalWorkItemRequest(
    Guid RunId,
    Guid? ManifestRowId,
    string WorkItemType,
    string? PartitionKey,
    int Priority,
    string? PayloadJson,
    DateTimeOffset? NotBeforeUtc);

public sealed record ClaimOperationalWorkItemsRequest(
    Guid RunId,
    string WorkerId,
    int MaxItems,
    int LeaseSeconds,
    string? PartitionKey);

public sealed record CompleteOperationalWorkItemRequest(
    Guid WorkItemId,
    string WorkerId,
    string? ResultJson);

public sealed record FailOperationalWorkItemRequest(
    Guid WorkItemId,
    string WorkerId,
    string ErrorCode,
    string ErrorMessage,
    bool IsRetryable,
    DateTimeOffset? NextAttemptUtc);

public sealed record ReleaseOperationalWorkItemRequest(
    Guid WorkItemId,
    string WorkerId,
    DateTimeOffset? NextAttemptUtc);

public sealed record OperationalWorkItemRecord(
    Guid WorkItemId,
    Guid RunId,
    Guid? ManifestRowId,
    string WorkItemType,
    string Status,
    string? PartitionKey,
    int Priority,
    int AttemptCount,
    int MaxAttempts,
    string? LeaseOwner,
    DateTimeOffset? LeaseExpiresUtc,
    DateTimeOffset? NotBeforeUtc,
    string? PayloadJson,
    string? ResultJson,
    string? LastErrorCode,
    string? LastErrorMessage,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

public sealed record OperationalWorkItemRunSummary(
    Guid RunId,
    int PendingCount,
    int LeasedCount,
    int CompletedCount,
    int FailedCount,
    int RetryableFailedCount,
    int TotalCount,
    DateTimeOffset? OldestPendingUtc,
    DateTimeOffset? NewestUpdateUtc);
