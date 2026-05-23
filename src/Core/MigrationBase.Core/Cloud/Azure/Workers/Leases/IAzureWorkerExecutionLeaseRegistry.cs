namespace MigrationBase.Core.Cloud.Azure.Workers.Leases;

public interface IAzureWorkerExecutionLeaseRegistry
{
    IReadOnlyCollection<AzureWorkerExecutionLeaseDescriptor> GetDescriptors();
    AzureWorkerExecutionLeaseDescriptor? FindByName(string leaseName);
}
