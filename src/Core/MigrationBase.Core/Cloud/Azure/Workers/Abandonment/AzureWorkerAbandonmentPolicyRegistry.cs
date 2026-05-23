namespace MigrationBase.Core.Cloud.Azure.Workers.Abandonment;

public sealed class AzureWorkerAbandonmentPolicyRegistry : IAzureWorkerAbandonmentPolicyRegistry
{
    private readonly IReadOnlyCollection<AzureWorkerAbandonmentPolicy> policies;

    public AzureWorkerAbandonmentPolicyRegistry(IEnumerable<AzureWorkerAbandonmentPolicy> policies)
    {
        this.policies = policies?.ToArray() ?? Array.Empty<AzureWorkerAbandonmentPolicy>();
    }

    public IReadOnlyCollection<AzureWorkerAbandonmentPolicy> GetPolicies() => policies;

    public AzureWorkerAbandonmentPolicy? FindPolicy(string hostRole)
    {
        if (string.IsNullOrWhiteSpace(hostRole))
        {
            return null;
        }

        return policies.FirstOrDefault(policy =>
            string.Equals(policy.HostRole, hostRole, StringComparison.OrdinalIgnoreCase));
    }
}
