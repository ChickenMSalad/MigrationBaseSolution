namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureSystemPairMetricsResponse
{
    public int TotalFailureCount { get; init; }

    public int SystemPairCount { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<OperationalGlobalFailureSystemPairDetailMetric> SystemPairs { get; init; } =
        Array.Empty<OperationalGlobalFailureSystemPairDetailMetric>();
}
