namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionWorkItemQueueSummary(
    Guid ExecutionSessionId,
    int Total,
    int Pending,
    int Leased,
    int Running,
    int Completed,
    int Failed,
    int DeadLettered);


