namespace MigrationBase.Core.Cloud.Azure.Workers.CircuitBreakers;

/// <summary>
/// Immutable in-memory registry for worker circuit breaker policies.
/// </summary>
public sealed class AzureWorkerCircuitBreakerRegistry : IAzureWorkerCircuitBreakerRegistry
{
    private readonly IReadOnlyDictionary<string, AzureWorkerCircuitBreakerPolicy> policies;

    public AzureWorkerCircuitBreakerRegistry(IEnumerable<AzureWorkerCircuitBreakerPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        this.policies = policies
            .Where(policy => !string.IsNullOrWhiteSpace(policy.Name))
            .GroupBy(policy => policy.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<AzureWorkerCircuitBreakerPolicy> GetPolicies()
    {
        return policies.Values.ToArray();
    }

    public AzureWorkerCircuitBreakerPolicy? FindPolicy(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return policies.TryGetValue(name, out var policy) ? policy : null;
    }
}
