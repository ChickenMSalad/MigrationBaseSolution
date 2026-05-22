namespace Migration.Admin.Api.Operational.Execution;

public sealed record TransitionExecutionPhaseRequest(
    Guid ExecutionSessionId,
    string NewPhase,
    string? Reason);
