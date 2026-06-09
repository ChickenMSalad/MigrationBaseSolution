namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherRunOnceResponse
{
    public string WorkerId { get; init; } = string.Empty;

    public int RequestedLeaseCount { get; init; }

    public int LeasedCount { get; init; }

    public int CompletedCount { get; init; }

    public int FailedCount { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyCollection<long> WorkItemIds { get; init; } =
        Array.Empty<long>();
}


