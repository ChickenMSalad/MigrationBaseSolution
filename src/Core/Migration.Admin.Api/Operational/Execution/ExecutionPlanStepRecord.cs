namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionPlanStepRecord(
    Guid ExecutionPlanStepId,
    Guid ExecutionSessionId,
    Guid? MigrationRunId,
    int StepOrder,
    string StepType,
    string StepName,
    string Status,
    string? SourceConnector,
    string? TargetConnector,
    string? PayloadJson,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? StartedUtc,
    DateTimeOffset? CompletedUtc,
    string? ErrorMessage);
