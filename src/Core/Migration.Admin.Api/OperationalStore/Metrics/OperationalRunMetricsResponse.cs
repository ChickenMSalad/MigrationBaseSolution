namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunMetricsResponse
{
    public int TotalCount { get; init; }

    public int CreatedCount { get; init; }

    public int StartedCount { get; init; }

    public int CompletedCount { get; init; }

    public int FailedCount { get; init; }

    public DateTimeOffset? OldestCreatedAt { get; init; }

    public DateTimeOffset? NewestCreatedAt { get; init; }

    public IReadOnlyCollection<OperationalRunStatusMetric> Statuses { get; init; } =
        Array.Empty<OperationalRunStatusMetric>();
}


