namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalActivityFeedResponse
{
    public int EventCount { get; init; }

    public int Limit { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<OperationalGlobalActivityEvent> Events { get; init; } =
        Array.Empty<OperationalGlobalActivityEvent>();
}
