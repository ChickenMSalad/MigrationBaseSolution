namespace Migration.Core.Azure.Topology;

/// <summary>
/// Configuration-bound registry of named Azure runtime topologies.
/// </summary>
public sealed class AzureRuntimeTopologyRegistryOptions
{
    public const string SectionName = "AzureRuntime:TopologyRegistry";

    public string? DefaultTopologyName { get; set; }

    public List<AzureRuntimeTopologyDescriptor> Topologies { get; set; } = new();
}
