namespace Migration.Admin.Api.Operational.Execution;

public sealed record EvaluateExecutionReplayAdmissionRequest(
    int? Take);

public sealed record ExecutionReplayAdmissionEvaluationResult(
    DateTimeOffset GeneratedUtc,
    int ActiveReplayCount,
    int MaxConcurrentReplays,
    bool WithinAllowedWindow,
    IReadOnlyList<ExecutionReplayAdmissionDecision> Decisions);

public sealed record ExecutionReplayAdmissionDecision(
    Guid ExecutionSessionId,
    string Name,
    string Decision,
    string Reason,
    DateTimeOffset CreatedUtc);

public sealed record ExecutionReplayAdmissionDecisionRecord(
    Guid ReplayAdmissionDecisionId,
    Guid ExecutionSessionId,
    string Decision,
    string Reason,
    int ActiveReplayCount,
    int MaxConcurrentReplays,
    bool WithinAllowedWindow,
    DateTimeOffset CreatedUtc);

public sealed record ExecutionReplayAdmissionDecisionHistoryResponse(
    Guid ExecutionSessionId,
    IReadOnlyList<ExecutionReplayAdmissionDecisionRecord> Decisions);


