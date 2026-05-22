namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineMetricsResponse
{
    public Guid RunId { get; init; }

    public int TotalEventCount { get; init; }

    public DateTimeOffset? FirstEventAt { get; init; }

    public DateTimeOffset? LastEventAt { get; init; }

    public IReadOnlyCollection<OperationalRunTimelineEventTypeMetric> EventTypes { get; init; } =
        Array.Empty<OperationalRunTimelineEventTypeMetric>();

    public IReadOnlyCollection<OperationalRunTimelineSourceMetric> Sources { get; init; } =
        Array.Empty<OperationalRunTimelineSourceMetric>();
}
