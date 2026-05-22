namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionSessionRecord(
    Guid ExecutionSessionId,
    Guid? MigrationRunId,
    string Name,
    string? SourceConnector,
    string? TargetConnector,
    string Status,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? StartedUtc,
    DateTimeOffset? CompletedUtc,
    string? Notes);
