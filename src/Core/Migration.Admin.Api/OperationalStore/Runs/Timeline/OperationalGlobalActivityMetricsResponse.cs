namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalActivityMetricsResponse
{
    public int TotalEventCount { get; init; }

    public DateTimeOffset? FirstEventAt { get; init; }

    public DateTimeOffset? LastEventAt { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<OperationalGlobalActivityEventTypeMetric> EventTypes { get; init; } =
        Array.Empty<OperationalGlobalActivityEventTypeMetric>();

    public IReadOnlyCollection<OperationalGlobalActivitySourceMetric> Sources { get; init; } =
        Array.Empty<OperationalGlobalActivitySourceMetric>();

    public IReadOnlyCollection<OperationalGlobalActivityRunMetric> Runs { get; init; } =
        Array.Empty<OperationalGlobalActivityRunMetric>();
}


