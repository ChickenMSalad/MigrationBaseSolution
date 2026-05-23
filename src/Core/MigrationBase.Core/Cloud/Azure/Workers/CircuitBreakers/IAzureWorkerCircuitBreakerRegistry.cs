namespace MigrationBase.Core.Cloud.Azure.Workers.CircuitBreakers;

/// <summary>
/// Provides circuit breaker policies available to Azure worker roles.
/// </summary>
public interface IAzureWorkerCircuitBreakerRegistry
{
    IReadOnlyCollection<AzureWorkerCircuitBreakerPolicy> GetPolicies();

    AzureWorkerCircuitBreakerPolicy? FindPolicy(string name);
}
