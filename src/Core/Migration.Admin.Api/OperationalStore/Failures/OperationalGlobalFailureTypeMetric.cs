namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureTypeMetric
{
    public string FailureType { get; init; } = string.Empty;

    public int Count { get; init; }

    public int RetriableCount { get; init; }

    public int NonRetriableCount { get; init; }
}


