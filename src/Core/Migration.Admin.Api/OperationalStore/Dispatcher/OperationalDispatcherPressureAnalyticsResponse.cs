
namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherPressureAnalyticsResponse
{
    public DateTimeOffset GeneratedAt { get; init; }

    public OperationalGlobalQueueDepthAnalyticsResponse QueueDepth { get; init; } = default!;

    public int PressureScore { get; init; }

    public string PressureLevel { get; init; } = string.Empty;

    public string PressureReason { get; init; } = string.Empty;

    public int OutstandingWorkItemCount { get; init; }

    public int LockedWorkItemCount { get; init; }

    public int FailedWorkItemCount { get; init; }

    public IReadOnlyCollection<string> Signals { get; init; } =
        Array.Empty<string>();
}
