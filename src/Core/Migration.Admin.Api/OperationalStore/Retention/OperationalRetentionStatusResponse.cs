namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRetentionStatusResponse
{
    public bool Enabled { get; init; }
    public int ArchiveAfterDays { get; init; }
    public int PurgeAfterDays { get; init; }
    public int BatchSize { get; init; }
    public DateTimeOffset ArchiveBefore { get; init; }
    public DateTimeOffset PurgeBefore { get; init; }
    public int EligibleArchiveRunCount { get; init; }
    public int EligiblePurgeRunCount { get; init; }
    public string Mode { get; init; } = string.Empty;
}


