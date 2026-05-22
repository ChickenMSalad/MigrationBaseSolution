namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionHistoryRetentionStatusResponse
{
    public bool Enabled { get; init; }

    public int PurgeAfterDays { get; init; }

    public int BatchSize { get; init; }

    public DateTimeOffset PurgeBefore { get; init; }

    public int EligiblePurgeExecutionCount { get; init; }

    public string Mode { get; init; } = string.Empty;
}
