namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureRunStatusDetailMetric
{
    public string RunStatus { get; init; } = string.Empty;

    public int Count { get; init; }

    public int RetriableCount { get; init; }

    public int NonRetriableCount { get; init; }

    public DateTimeOffset? FirstFailureAt { get; init; }

    public DateTimeOffset? LastFailureAt { get; init; }

    public IReadOnlyCollection<OperationalGlobalFailureTypeMetric> FailureTypes { get; init; } =
        Array.Empty<OperationalGlobalFailureTypeMetric>();
}
