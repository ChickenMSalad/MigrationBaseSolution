namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherWorkerExecutionMetric
{
    public string WorkerId { get; init; } = string.Empty;

    public int ExecutionCount { get; init; }

    public int TotalLeasedCount { get; init; }

    public int TotalCompletedCount { get; init; }

    public int TotalFailedCount { get; init; }

    public DateTimeOffset? LastStartedAt { get; init; }
}


