namespace MigrationBase.Core.Cloud.Azure.Workers.Leases;

public sealed class AzureWorkerExecutionLeaseRegistry : IAzureWorkerExecutionLeaseRegistry
{
    private readonly IReadOnlyDictionary<string, AzureWorkerExecutionLeaseDescriptor> descriptors;

    public AzureWorkerExecutionLeaseRegistry(IEnumerable<AzureWorkerExecutionLeaseDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        this.descriptors = descriptors
            .Where(descriptor => !string.IsNullOrWhiteSpace(descriptor.LeaseName))
            .GroupBy(descriptor => descriptor.LeaseName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<AzureWorkerExecutionLeaseDescriptor> GetDescriptors() => descriptors.Values.ToArray();

    public AzureWorkerExecutionLeaseDescriptor? FindByName(string leaseName)
    {
        if (string.IsNullOrWhiteSpace(leaseName))
        {
            return null;
        }

        return descriptors.TryGetValue(leaseName, out var descriptor) ? descriptor : null;
    }
}
