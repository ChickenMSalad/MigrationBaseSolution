namespace Migration.Admin.Api.Operational.Execution;

public sealed record PrepareExecutionReplayRequest(
    Guid ExecutionSessionId,
    string Scope,
    string? Reason);

public sealed record ExecutionReplayPreparationResult(
    Guid SourceExecutionSessionId,
    DateTimeOffset GeneratedUtc,
    string Scope,
    bool RequiresApproval,
    bool CanPrepareReplay,
    string Recommendation,
    IReadOnlyList<ExecutionReplayPreparationItem> Items,
    IReadOnlyList<ExecutionReplayFinding> Findings);

public sealed record ExecutionReplayPreparationItem(
    Guid? SourceExecutionWorkItemId,
    Guid? SourceExecutionPlanStepId,
    int ReplayOrder,
    string ReplayType,
    string ReplayName,
    string SourceStatus,
    string? PayloadJson);


