namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionRecord
{
    public Guid ExecutionId { get; init; }

    public string WorkerId { get; init; } = string.Empty;

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset CompletedAt { get; init; }

    public long DurationMilliseconds { get; init; }

    public int RequestedLeaseCount { get; init; }

    public int LeasedCount { get; init; }

    public int CompletedCount { get; init; }

    public int FailedCount { get; init; }

    public string Outcome { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
