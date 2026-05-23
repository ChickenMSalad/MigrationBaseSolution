namespace MigrationBase.Core.Cloud.Azure.Workers.Abandonment;

/// <summary>
/// Classifies why a worker released or abandoned work before successful completion.
/// </summary>
public enum AzureWorkerAbandonmentReason
{
    Unknown = 0,
    GracefulShutdown = 1,
    LeaseExpired = 2,
    LeaseLost = 3,
    HeartbeatStale = 4,
    CancellationRequested = 5,
    RetryBudgetExceeded = 6,
    PoisonDetected = 7,
    CapacityPressure = 8,
    OperatorRequested = 9
}
