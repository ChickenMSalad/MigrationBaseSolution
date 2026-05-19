namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineEventTypeMetric
{
    public string EventType { get; init; } = string.Empty;

    public int Count { get; init; }
}
