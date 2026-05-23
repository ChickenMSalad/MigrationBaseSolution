namespace MigrationBase.Core.Cloud.Azure.Workers.Abandonment;

public interface IAzureWorkerAbandonmentPolicyRegistry
{
    IReadOnlyCollection<AzureWorkerAbandonmentPolicy> GetPolicies();

    AzureWorkerAbandonmentPolicy? FindPolicy(string hostRole);
}
