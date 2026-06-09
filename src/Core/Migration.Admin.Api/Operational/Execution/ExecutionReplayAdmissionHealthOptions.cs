namespace Migration.Admin.Api.Operational.Execution;

public sealed class ExecutionReplayAdmissionHealthOptions
{
    public const string SectionName = "ExecutionReplayAdmissionHealth";

    public int StalePendingMinutes { get; set; } = 60;

    public int Take { get; set; } = 50;
}


