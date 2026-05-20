namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureRunStatusMetricsResponse
{
    public int TotalFailureCount { get; init; }

    public int RunStatusCount { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<OperationalGlobalFailureRunStatusDetailMetric> RunStatuses { get; init; } =
        Array.Empty<OperationalGlobalFailureRunStatusDetailMetric>();
}
