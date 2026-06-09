namespace Migration.Admin.Api.Operational.Execution;

public sealed record ApproveExecutionReplayRequest(
    Guid SourceExecutionSessionId,
    string Scope,
    string ApprovedBy,
    string ApprovalNote,
    int ExpiresInMinutes);

public sealed record ExecutionReplayApprovalRecord(
    Guid ReplayApprovalId,
    Guid SourceExecutionSessionId,
    string Scope,
    string ApprovedBy,
    string ApprovalNote,
    string Status,
    DateTimeOffset ExpiresUtc,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? ConsumedUtc,
    Guid? ReplayExecutionSessionId);

public sealed record ExecutionReplayApprovalResult(
    ExecutionReplayApprovalRecord Approval,
    IReadOnlyList<ExecutionReplayFinding> Findings);


