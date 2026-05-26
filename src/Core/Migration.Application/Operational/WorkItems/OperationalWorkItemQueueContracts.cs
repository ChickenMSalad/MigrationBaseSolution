namespace Migration.Application.Operational.WorkItems;

public interface IOperationalWorkItemQueue
{
    Task<OperationalWorkItemRecord> EnqueueAsync(EnqueueOperationalWorkItemRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalWorkItemRecord>> ClaimAsync(ClaimOperationalWorkItemsRequest request, CancellationToken cancellationToken = default);

    Task<OperationalWorkItemRecord?> GetAsync(long workItemId, CancellationToken cancellationToken = default);

    Task<OperationalWorkItemRunSummary> GetRunSummaryAsync(Guid runId, CancellationToken cancellationToken = default);

    Task CompleteAsync(CompleteOperationalWorkItemRequest request, CancellationToken cancellationToken = default);

    Task FailAsync(FailOperationalWorkItemRequest request, CancellationToken cancellationToken = default);

    Task ReleaseAsync(ReleaseOperationalWorkItemRequest request, CancellationToken cancellationToken = default);
}


public sealed record EnqueueOperationalWorkItemRequest(
    Guid RunId,
    long? ManifestRowId,
    string WorkItemType,
    string? PartitionKey,
    int Priority,
    string? PayloadJson,
    DateTimeOffset? NotBeforeUtc);

public sealed record CompleteOperationalWorkItemRequest(
    long WorkItemId,
    string WorkerId,
    string? ResultJson);

public sealed record FailOperationalWorkItemRequest(
    long WorkItemId,
    string WorkerId,
    string ErrorCode,
    string ErrorMessage,
    bool IsRetryable,
    DateTimeOffset? NextAttemptUtc);

public sealed record ReleaseOperationalWorkItemRequest(
    long WorkItemId,
    string WorkerId,
    DateTimeOffset? NextAttemptUtc);

public sealed class OperationalWorkItemRecord
{
    public long WorkItemId { get; set; }

    public Guid RunId { get; set; }

    public long? ManifestRowId { get; set; }

    public string WorkItemType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? PartitionKey { get; set; }

    public int Priority { get; set; }

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; }

    public string? LeaseOwner { get; set; }

    public DateTimeOffset? LeaseExpiresUtc { get; set; }

    public DateTimeOffset? NotBeforeUtc { get; set; }

    public string? PayloadJson { get; set; }

    public string? ResultJson { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }
}

public sealed record ClaimOperationalWorkItemsRequest(
    Guid RunId,
    string WorkerId,
    int MaxItems,
    int LeaseSeconds,
    string? PartitionKey);


public sealed class OperationalWorkItemRunSummary
{
    public Guid RunId { get; set; }

    public int PendingCount { get; set; }

    public int LeasedCount { get; set; }

    public int CompletedCount { get; set; }

    public int FailedCount { get; set; }

    public int RetryableFailedCount { get; set; }

    public int TotalCount { get; set; }

    public DateTimeOffset? OldestPendingUtc { get; set; }

    public DateTimeOffset? NewestUpdateUtc { get; set; }
}
