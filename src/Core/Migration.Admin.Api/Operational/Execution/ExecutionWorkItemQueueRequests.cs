namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExpandExecutionPlanToWorkItemsRequest(
    Guid ExecutionSessionId);

public sealed record LeaseExecutionWorkItemsRequest(
    Guid ExecutionSessionId,
    string WorkerId,
    int Take,
    int LeaseSeconds);

public sealed record RenewExecutionWorkItemLeaseRequest(
    Guid ExecutionWorkItemId,
    Guid LeaseId,
    string WorkerId,
    int LeaseSeconds);

public sealed record RequeueExecutionWorkItemsRequest(
    Guid ExecutionSessionId,
    bool IncludeFailed,
    bool IncludeExpiredLeases);

public sealed record CompleteExecutionWorkItemRequest(
    Guid ExecutionWorkItemId,
    Guid LeaseId,
    string WorkerId);

public sealed record FailExecutionWorkItemRequest(
    Guid ExecutionWorkItemId,
    Guid LeaseId,
    string WorkerId,
    string ErrorMessage);
