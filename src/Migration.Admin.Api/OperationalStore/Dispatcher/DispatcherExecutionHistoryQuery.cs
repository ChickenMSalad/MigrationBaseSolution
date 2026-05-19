namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionHistoryQuery
{
    public string? WorkerId { get; init; }

    public string? Outcome { get; init; }

    public int Limit { get; init; } = 50;
}
