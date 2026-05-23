namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionWorkerHeartbeatRecord(
    string WorkerId,
    Guid? ExecutionSessionId,
    string Status,
    DateTimeOffset LastHeartbeatUtc,
    int ActiveLeaseCount,
    string? Message,
    DateTimeOffset CreatedUtc);
