namespace MigrationBase.Core.Cloud.Azure.Workers.Retry;

public sealed class AzureWorkerRetryPolicyRegistry : IAzureWorkerRetryPolicyRegistry
{
    private readonly IReadOnlyList<AzureWorkerRetryPolicyDescriptor> _policies;

    public AzureWorkerRetryPolicyRegistry(IEnumerable<AzureWorkerRetryPolicyDescriptor> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);
        _policies = policies.ToArray();
    }

    public IReadOnlyList<AzureWorkerRetryPolicyDescriptor> GetPolicies() => _policies;

    public AzureWorkerRetryPolicyDescriptor? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _policies.FirstOrDefault(policy => string.Equals(policy.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public AzureWorkerRetryPolicyDescriptor? FindForWorkloadRole(string workloadRole)
    {
        if (string.IsNullOrWhiteSpace(workloadRole))
        {
            return null;
        }

        return _policies.FirstOrDefault(policy => string.Equals(policy.WorkloadRole, workloadRole, StringComparison.OrdinalIgnoreCase));
    }
}
