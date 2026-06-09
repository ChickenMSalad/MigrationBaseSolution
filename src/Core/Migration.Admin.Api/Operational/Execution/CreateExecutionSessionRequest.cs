namespace Migration.Admin.Api.Operational.Execution;

public sealed record CreateExecutionSessionRequest(
    Guid? MigrationRunId,
    string Name,
    string? SourceConnector,
    string? TargetConnector,
    string? Notes);


