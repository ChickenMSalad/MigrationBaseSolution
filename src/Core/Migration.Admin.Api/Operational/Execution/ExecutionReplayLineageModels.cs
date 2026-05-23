namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionReplayLineageResult(
    Guid ExecutionSessionId,
    Guid RootExecutionSessionId,
    Guid? SourceExecutionSessionId,
    int ReplayDepth,
    string? ReplayScope,
    IReadOnlyList<ExecutionReplayLineageNode> Ancestors,
    IReadOnlyList<ExecutionReplayLineageNode> Children);

public sealed record ExecutionReplayLineageNode(
    Guid ExecutionSessionId,
    Guid? ReplaySourceExecutionSessionId,
    string Name,
    string Status,
    string? ReplayScope,
    int ReplayDepth,
    DateTimeOffset CreatedUtc);
