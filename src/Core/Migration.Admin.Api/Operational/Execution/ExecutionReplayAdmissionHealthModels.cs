namespace Migration.Admin.Api.Operational.Execution;

public sealed record EvaluateExecutionReplayAdmissionHealthRequest(
    bool EmitEvents,
    int? Take);

public sealed record ExecutionReplayAdmissionHealthResult(
    DateTimeOffset GeneratedUtc,
    int StalePendingMinutes,
    int PendingCount,
    int StalePendingCount,
    IReadOnlyList<ExecutionReplayAdmissionStalePendingSession> StalePendingSessions);

public sealed record ExecutionReplayAdmissionStalePendingSession(
    Guid ExecutionSessionId,
    string Name,
    string Status,
    string? ReplayScope,
    int ReplayDepth,
    DateTimeOffset CreatedUtc,
    int AgeMinutes);


