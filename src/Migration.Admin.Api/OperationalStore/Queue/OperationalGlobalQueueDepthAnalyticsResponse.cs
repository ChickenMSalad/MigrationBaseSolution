namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalQueueDepthAnalyticsResponse
{
    public int TotalWorkItemCount { get; init; }

    public int OutstandingWorkItemCount { get; init; }

    public int LockedWorkItemCount { get; init; }

    public int CompletedWorkItemCount { get; init; }

    public int FailedWorkItemCount { get; init; }

    public decimal CompletionPercent { get; init; }

    public int QueuePressureScore { get; init; }

    public string QueuePressureLevel { get; init; } = string.Empty;

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<OperationalGlobalQueueDepthStatusMetric> Statuses { get; init; } =
        Array.Empty<OperationalGlobalQueueDepthStatusMetric>();

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}
