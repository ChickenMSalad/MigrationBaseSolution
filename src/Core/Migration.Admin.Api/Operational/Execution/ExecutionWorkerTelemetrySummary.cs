namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionWorkerTelemetrySummary(
    int TotalWorkers,
    int ActiveWorkers,
    int IdleWorkers,
    int StaleWorkers,
    DateTimeOffset GeneratedUtc,
    IReadOnlyList<ExecutionWorkerHeartbeatRecord> Workers);
