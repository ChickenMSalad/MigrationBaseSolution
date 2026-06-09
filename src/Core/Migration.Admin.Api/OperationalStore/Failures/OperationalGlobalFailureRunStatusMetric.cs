namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureRunStatusMetric
{
    public string RunStatus { get; init; } = string.Empty;

    public int Count { get; init; }
}


