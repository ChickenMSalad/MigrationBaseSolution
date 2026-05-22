namespace Migration.Admin.Api.Operational.Execution;

public sealed record SeedExecutionPlanRequest(
    Guid ExecutionSessionId,
    string? SourceConnector,
    string? TargetConnector);
