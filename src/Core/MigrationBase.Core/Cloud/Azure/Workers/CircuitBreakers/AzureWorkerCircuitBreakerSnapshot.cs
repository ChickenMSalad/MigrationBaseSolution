namespace MigrationBase.Core.Cloud.Azure.Workers.CircuitBreakers;

/// <summary>
/// Captures the current observed state of a worker circuit breaker.
/// </summary>
public sealed record AzureWorkerCircuitBreakerSnapshot
{
    public required string BreakerName { get; init; }

    public required AzureWorkerCircuitBreakerScope Scope { get; init; }

    public AzureWorkerCircuitBreakerState State { get; init; } = AzureWorkerCircuitBreakerState.Unknown;

    public DateTimeOffset ObservedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? OpenedAtUtc { get; init; }

    public DateTimeOffset? NextProbeAtUtc { get; init; }

    public int FailureCount { get; init; }

    public int ConsecutiveFailureCount { get; init; }

    public int SuccessCount { get; init; }

    public string? Reason { get; init; }
}
