namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionReplayMaterializationResult(
    Guid SourceExecutionSessionId,
    Guid ReplayExecutionSessionId,
    string Scope,
    int ReplayDepth,
    int WorkItemCount,
    DateTimeOffset CreatedUtc,
    IReadOnlyList<ExecutionReplayFinding> Findings);


