namespace Migration.Core.Azure.Topology;

/// <summary>
/// Lightweight in-memory registry used by hosts and tools after configuration binding.
/// </summary>
public sealed class InMemoryAzureRuntimeTopologyRegistry : IAzureRuntimeTopologyRegistry
{
    private readonly IReadOnlyList<AzureRuntimeTopologyDescriptor> topologies;
    private readonly string? defaultTopologyName;

    public InMemoryAzureRuntimeTopologyRegistry(AzureRuntimeTopologyRegistryOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        topologies = options.Topologies
            .Where(static topology => !string.IsNullOrWhiteSpace(topology.Name))
            .ToArray();

        defaultTopologyName = string.IsNullOrWhiteSpace(options.DefaultTopologyName)
            ? null
            : options.DefaultTopologyName.Trim();
    }

    public IReadOnlyCollection<AzureRuntimeTopologyDescriptor> GetAll() => topologies;

    public AzureRuntimeTopologyDescriptor? GetDefault()
    {
        if (!string.IsNullOrWhiteSpace(defaultTopologyName))
        {
            var named = FindByName(defaultTopologyName);
            if (named is not null)
            {
                return named;
            }
        }

        return topologies.Count == 1 ? topologies[0] : null;
    }

    public AzureRuntimeTopologyDescriptor? FindByName(string topologyName)
    {
        if (string.IsNullOrWhiteSpace(topologyName))
        {
            return null;
        }

        return topologies.FirstOrDefault(topology =>
            string.Equals(topology.Name, topologyName.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
