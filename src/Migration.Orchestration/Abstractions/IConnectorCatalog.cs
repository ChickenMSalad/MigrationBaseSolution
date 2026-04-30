using Migration.Orchestration.Descriptors;

namespace Migration.Orchestration.Abstractions;

public interface IConnectorCatalog
{
    IReadOnlyList<ConnectorDescriptor> GetSources();
    IReadOnlyList<ConnectorDescriptor> GetTargets();
    IReadOnlyList<ManifestProviderDescriptor> GetManifestProviders();
}
