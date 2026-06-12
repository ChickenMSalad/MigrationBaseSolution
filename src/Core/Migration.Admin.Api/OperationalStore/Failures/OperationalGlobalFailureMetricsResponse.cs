namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureMetricsResponse
{
    public int TotalFailureCount { get; init; }

    public int RetriableFailureCount { get; init; }

    public int NonRetriableFailureCount { get; init; }

    public DateTimeOffset? FirstFailureAt { get; init; }

    public DateTimeOffset? LastFailureAt { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<OperationalGlobalFailureTypeMetric> FailureTypes { get; init; } =
        Array.Empty<OperationalGlobalFailureTypeMetric>();

    public IReadOnlyCollection<OperationalGlobalFailureRunStatusMetric> RunStatuses { get; init; } =
        Array.Empty<OperationalGlobalFailureRunStatusMetric>();

    public IReadOnlyCollection<OperationalGlobalFailureSystemPairMetric> SystemPairs { get; init; } =
        Array.Empty<OperationalGlobalFailureSystemPairMetric>();
}


