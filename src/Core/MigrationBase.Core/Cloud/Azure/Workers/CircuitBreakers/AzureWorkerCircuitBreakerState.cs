namespace MigrationBase.Core.Cloud.Azure.Workers.CircuitBreakers;

/// <summary>
/// Describes the runtime state of a worker circuit breaker.
/// </summary>
public enum AzureWorkerCircuitBreakerState
{
    Unknown = 0,
    Closed = 1,
    Open = 2,
    HalfOpen = 3,
    Disabled = 4
}
