namespace Migration.Admin.Api.Operational.SqlMetrics;

public sealed record SqlOperationalMetricsSnapshot(
    string Status,
    int ActiveRuns,
    int QueueDepth,
    int FailureCount,
    int ActiveWorkers,
    int SlaSloBreaches,
    decimal EstimatedHoursRemaining,
    decimal EstimatedMonthlyCost,
    string? Message);


