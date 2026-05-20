namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalActivityQuery
{
    public Guid? RunId { get; init; }

    public string? EventType { get; init; }

    public string? Source { get; init; }

    public string? SearchText { get; init; }

    public int Limit { get; init; } = 50;
}
