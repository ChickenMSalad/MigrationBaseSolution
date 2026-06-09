namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineQuery
{
    public string? EventType { get; init; }

    public string? Source { get; init; }

    public int Limit { get; init; } = 100;
}


