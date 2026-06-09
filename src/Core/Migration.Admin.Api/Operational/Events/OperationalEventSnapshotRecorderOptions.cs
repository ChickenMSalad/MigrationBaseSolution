namespace Migration.Admin.Api.Operational.Events;

public sealed class OperationalEventSnapshotRecorderOptions
{
    public const string SectionName = "OperationalEventSnapshots";

    public bool Enabled { get; set; }

    public int IntervalSeconds { get; set; } = 300;

    public int StartupDelaySeconds { get; set; } = 30;
}


