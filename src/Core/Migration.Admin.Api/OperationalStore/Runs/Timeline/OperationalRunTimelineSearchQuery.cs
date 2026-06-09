namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineSearchQuery
{
    public string SearchText { get; init; } = string.Empty;

    public int Limit { get; init; } = 100;
}


