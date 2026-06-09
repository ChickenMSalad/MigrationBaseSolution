namespace Migration.Admin.Api.Operational.Events;

public sealed class OperationalEventRetentionOptions
{
    public const string SectionName = "OperationalEventRetention";

    public bool Enabled { get; set; }

    public int RetentionDays { get; set; } = 30;

    public int IntervalHours { get; set; } = 24;

    public int StartupDelaySeconds { get; set; } = 60;
}


