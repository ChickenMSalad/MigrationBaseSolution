namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionReplayAdmissionBackgroundStatus(
    bool Enabled,
    int IntervalSeconds,
    int Take,
    bool AdmissionEnabled,
    int MaxConcurrentReplays,
    int AllowedStartHourUtc,
    int AllowedEndHourUtc,
    DateTimeOffset GeneratedUtc);


