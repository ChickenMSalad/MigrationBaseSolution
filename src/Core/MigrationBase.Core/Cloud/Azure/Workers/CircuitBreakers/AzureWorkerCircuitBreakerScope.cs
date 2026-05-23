namespace MigrationBase.Core.Cloud.Azure.Workers.CircuitBreakers;

/// <summary>
/// Defines the operational scope protected by a circuit breaker.
/// </summary>
public sealed record AzureWorkerCircuitBreakerScope
{
    public required string EnvironmentName { get; init; }

    public required string HostRole { get; init; }

    public required string WorkloadName { get; init; }

    public string? QueueName { get; init; }

    public string? TenantKey { get; init; }

    public string? MigrationRunId { get; init; }
}
