namespace Migration.Admin.Api.Operational.Execution;

public sealed class ExecutionReplayAdmissionOptions
{
    public const string SectionName = "ExecutionReplayAdmission";

    public bool Enabled { get; set; } = true;

    public int MaxConcurrentReplays { get; set; } = 1;

    public int AllowedStartHourUtc { get; set; } = 0;

    public int AllowedEndHourUtc { get; set; } = 24;

    public int Take { get; set; } = 25;
}
