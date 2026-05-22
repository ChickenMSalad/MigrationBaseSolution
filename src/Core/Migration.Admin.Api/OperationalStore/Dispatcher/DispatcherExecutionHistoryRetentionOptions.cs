namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionHistoryRetentionOptions
{
    public const string SectionName = "DispatcherExecutionHistoryRetention";

    public bool Enabled { get; init; }

    public int PurgeAfterDays { get; init; } = 30;

    public int BatchSize { get; init; } = 1000;
}
