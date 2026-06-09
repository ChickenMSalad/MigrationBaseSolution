namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineResponse
{
    public Guid RunId { get; init; }

    public int EventCount { get; init; }

    public IReadOnlyCollection<OperationalRunTimelineEvent> Events { get; init; } =
        Array.Empty<OperationalRunTimelineEvent>();
}


