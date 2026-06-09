namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalLeaseMetricsResponse
{
    public int LeaseTimeoutMinutes { get; init; }

    public int LockedCount { get; init; }

    public int ExpiredCount { get; init; }

    public DateTimeOffset? OldestLockedAt { get; init; }

    public string? OldestLockedBy { get; init; }

    public int DistinctWorkerCount { get; init; }

    public IReadOnlyCollection<OperationalLeaseWorkerMetric> Workers { get; init; } =
        Array.Empty<OperationalLeaseWorkerMetric>();
}


