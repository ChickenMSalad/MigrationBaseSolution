namespace Migration.Admin.Api.Operational.Execution;

public sealed record PauseExecutionSessionRequest(
    Guid ExecutionSessionId,
    string? Reason);

public sealed record ResumeExecutionSessionRequest(
    Guid ExecutionSessionId,
    string? Reason);

public sealed record CancelExecutionSessionRequest(
    Guid ExecutionSessionId,
    string? Reason);


