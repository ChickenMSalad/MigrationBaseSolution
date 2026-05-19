namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionHistoryRetentionPurgeResponse
{
    public bool Enabled { get; init; }

    public bool Executed { get; init; }

    public int PurgedExecutionCount { get; init; }

    public DateTimeOffset PurgeBefore { get; init; }

    public string Message { get; init; } = string.Empty;
}
