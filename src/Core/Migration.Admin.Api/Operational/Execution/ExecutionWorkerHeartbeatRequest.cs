namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionWorkerHeartbeatRequest(
    string WorkerId,
    Guid? ExecutionSessionId,
    string Status,
    int ActiveLeaseCount,
    string? Message);


