namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalActivityRunMetric
{
    public Guid RunId { get; init; }

    public int Count { get; init; }

    public DateTimeOffset? LastEventAt { get; init; }
}


