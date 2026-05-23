namespace Migration.Admin.Api.Operational.Execution;

public sealed class ExecutionReplayAdmissionBackgroundOptions
{
    public const string SectionName = "ExecutionReplayAdmissionBackground";

    public bool Enabled { get; set; } = false;

    public int IntervalSeconds { get; set; } = 60;

    public int Take { get; set; } = 25;
}
