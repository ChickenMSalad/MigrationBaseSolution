namespace MigrationBase.Core.Cloud.Azure.Workers.Retry;

public interface IAzureWorkerRetryPolicyRegistry
{
    IReadOnlyList<AzureWorkerRetryPolicyDescriptor> GetPolicies();

    AzureWorkerRetryPolicyDescriptor? FindByName(string name);

    AzureWorkerRetryPolicyDescriptor? FindForWorkloadRole(string workloadRole);
}
