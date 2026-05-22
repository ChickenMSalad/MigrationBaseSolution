namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthSummaryResponse
{
    public int TotalRunCount { get; init; }

    public int ActiveRunCount { get; init; }

    public int CompletedRunCount { get; init; }

    public int FailedRunCount { get; init; }

    public int CancelRequestedRunCount { get; init; }

    public int AbortedRunCount { get; init; }

    public int TotalWorkItemCount { get; init; }

    public int OutstandingWorkItemCount { get; init; }

    public int LockedWorkItemCount { get; init; }

    public int CompletedWorkItemCount { get; init; }

    public int FailedWorkItemCount { get; init; }

    public int TotalFailureCount { get; init; }

    public decimal CompletionPercent { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<OperationalGlobalRunHealthStatusMetric> RunStatuses { get; init; } =
        Array.Empty<OperationalGlobalRunHealthStatusMetric>();

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}
