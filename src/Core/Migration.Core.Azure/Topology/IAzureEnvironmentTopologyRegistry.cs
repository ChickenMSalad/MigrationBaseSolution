using System.Collections.Generic;

namespace Migration.Core.Azure.Topology;

public interface IAzureEnvironmentTopologyRegistry
{
    IReadOnlyCollection<AzureEnvironmentTopologyDescriptor> GetAll();

    AzureEnvironmentTopologyDescriptor? TryGet(string environmentName);

    AzureEnvironmentTopologyValidationResult Validate(AzureEnvironmentTopologyDescriptor descriptor);
}
