namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRecentFailuresResponse
{
    public int Count { get; init; }

    public int Limit { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<OperationalGlobalFailureItem> Failures { get; init; } =
        Array.Empty<OperationalGlobalFailureItem>();
}


