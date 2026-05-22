namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalQueueDepthStatusMetric
{
    public string Status { get; init; } = string.Empty;

    public int Count { get; init; }
}
