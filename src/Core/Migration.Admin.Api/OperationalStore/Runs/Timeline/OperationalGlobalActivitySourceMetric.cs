namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalActivitySourceMetric
{
    public string Source { get; init; } = string.Empty;

    public int Count { get; init; }
}
