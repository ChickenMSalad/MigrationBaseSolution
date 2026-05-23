namespace Migration.Admin.Api.Operational.Execution;

public sealed record OverrideExecutionReplayPolicyRequest(
    Guid SourceExecutionSessionId,
    string Scope,
    string OverriddenBy,
    string OverrideReason,
    int ExpiresInMinutes);

public sealed record ExecutionReplayPolicyOverrideRecord(
    Guid ReplayPolicyOverrideId,
    Guid SourceExecutionSessionId,
    string Scope,
    string PolicyDecision,
    int PolicyScore,
    string OverriddenBy,
    string OverrideReason,
    string Status,
    DateTimeOffset ExpiresUtc,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? ConsumedUtc,
    Guid? ReplayExecutionSessionId);

public sealed record ExecutionReplayPolicyOverrideResult(
    ExecutionReplayPolicyOverrideRecord Override,
    IReadOnlyList<ExecutionReplayPolicyViolation> Violations);

public sealed record ExecutionReplayPolicyOverrideHistoryResponse(
    Guid SourceExecutionSessionId,
    IReadOnlyList<ExecutionReplayPolicyOverrideRecord> Overrides);
