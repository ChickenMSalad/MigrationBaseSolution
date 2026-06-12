namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthStatusMetric
{
    public string RunStatus { get; init; } = string.Empty;

    public int Count { get; init; }
}


