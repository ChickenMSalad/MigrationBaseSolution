namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionHistoryMetricsResponse
{
    public int TotalExecutionCount { get; init; }

    public int CompletedExecutionCount { get; init; }

    public int CompletedWithFailuresExecutionCount { get; init; }

    public int FailedExecutionCount { get; init; }

    public int TotalLeasedCount { get; init; }

    public int TotalCompletedCount { get; init; }

    public int TotalFailedCount { get; init; }

    public decimal AverageDurationMilliseconds { get; init; }

    public DateTimeOffset? OldestExecutionStartedAt { get; init; }

    public DateTimeOffset? NewestExecutionStartedAt { get; init; }

    public IReadOnlyCollection<DispatcherWorkerExecutionMetric> Workers { get; init; } =
        Array.Empty<DispatcherWorkerExecutionMetric>();
}
