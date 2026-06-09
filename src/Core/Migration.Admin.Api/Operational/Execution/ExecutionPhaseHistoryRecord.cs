namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionPhaseHistoryRecord(
    Guid ExecutionPhaseHistoryId,
    Guid ExecutionSessionId,
    Guid? MigrationRunId,
    string? PreviousPhase,
    string NewPhase,
    string? Reason,
    DateTimeOffset CreatedUtc);


