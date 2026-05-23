namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionReplayPolicyEvaluationResult(
    Guid SourceExecutionSessionId,
    string Scope,
    DateTimeOffset GeneratedUtc,
    string Decision,
    int PolicyScore,
    IReadOnlyList<ExecutionReplayPolicyViolation> Violations,
    ExecutionReplayPolicyMetrics Metrics);

public sealed record ExecutionReplayPolicyViolation(
    string Severity,
    string Code,
    string Message);

public sealed record ExecutionReplayPolicyMetrics(
    int ReplayDepth,
    int PreparedItemCount,
    int TotalWorkItemCount,
    int FailedWorkItemCount,
    int DeadLetteredWorkItemCount,
    int ActiveReplayCount,
    decimal DeadLetteredPercent);
