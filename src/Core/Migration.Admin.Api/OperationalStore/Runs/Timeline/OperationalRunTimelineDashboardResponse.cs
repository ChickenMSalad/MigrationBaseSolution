namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineDashboardResponse
{
    public Guid RunId { get; init; }

    public OperationalRunDashboardSummaryResponse RunDashboard { get; init; } = default!;

    public OperationalRunTimelineMetricsResponse TimelineMetrics { get; init; } = default!;

    public OperationalRunTimelineResponse TimelinePreview { get; init; } = default!;

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}


