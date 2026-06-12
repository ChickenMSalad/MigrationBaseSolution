namespace Migration.Admin.Api.Operational.Execution;

public sealed record MaterializeExecutionReplayRequest(
    Guid SourceExecutionSessionId,
    string Scope,
    string ApprovalNote);


