namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalWorkItemMetricsResponse
{
    public int TotalCount { get; init; }

    public int CreatedCount { get; init; }

    public int LockedCount { get; init; }

    public int ProcessingCount { get; init; }

    public int CompletedCount { get; init; }

    public int FailedCount { get; init; }

    public decimal AverageAttemptCount { get; init; }

    public DateTimeOffset? OldestCreatedAt { get; init; }

    public DateTimeOffset? OldestLockedAt { get; init; }

    public int ExpiredLeaseCount { get; init; }
}
