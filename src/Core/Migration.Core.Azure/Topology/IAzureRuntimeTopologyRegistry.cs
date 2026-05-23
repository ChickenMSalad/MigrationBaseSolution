namespace Migration.Core.Azure.Topology;

public interface IAzureRuntimeTopologyRegistry
{
    IReadOnlyCollection<AzureRuntimeTopologyDescriptor> GetAll();

    AzureRuntimeTopologyDescriptor? GetDefault();

    AzureRuntimeTopologyDescriptor? FindByName(string topologyName);
}
