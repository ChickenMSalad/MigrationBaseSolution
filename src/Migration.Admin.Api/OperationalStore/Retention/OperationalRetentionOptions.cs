namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRetentionOptions
{
    public const string SectionName = "OperationalRetention";
    public bool Enabled { get; init; }
    public int ArchiveAfterDays { get; init; } = 30;
    public int PurgeAfterDays { get; init; } = 180;
    public int BatchSize { get; init; } = 100;
}
