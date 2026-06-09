namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalLeaseWorkerMetric
{
    public string WorkerId { get; init; } = string.Empty;

    public int LockedCount { get; init; }

    public DateTimeOffset? OldestLockedAt { get; init; }

    public DateTimeOffset? NewestLockedAt { get; init; }
}


