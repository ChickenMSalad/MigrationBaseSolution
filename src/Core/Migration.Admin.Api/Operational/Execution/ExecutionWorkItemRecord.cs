namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionWorkItemRecord(
    Guid ExecutionWorkItemId,
    Guid ExecutionSessionId,
    Guid? MigrationRunId,
    Guid? ExecutionPlanStepId,
    string WorkItemType,
    string WorkItemName,
    string Status,
    int Priority,
    int RetryCount,
    int MaxRetries,
    string? WorkerId,
    Guid? LeaseId,
    DateTimeOffset? LeaseExpiresUtc,
    string? PayloadJson,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? StartedUtc,
    DateTimeOffset? CompletedUtc,
    string? ErrorMessage);
