using System;
using System.Collections.Generic;
using System.Linq;

namespace Migration.Core.Azure.Topology;

public sealed class AzureEnvironmentTopologyRegistry : IAzureEnvironmentTopologyRegistry
{
    private readonly IReadOnlyDictionary<string, AzureEnvironmentTopologyDescriptor> _topologies;

    public AzureEnvironmentTopologyRegistry(IEnumerable<AzureEnvironmentTopologyDescriptor> topologies)
    {
        ArgumentNullException.ThrowIfNull(topologies);

        _topologies = topologies
            .Where(x => !string.IsNullOrWhiteSpace(x.EnvironmentName))
            .GroupBy(x => x.EnvironmentName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<AzureEnvironmentTopologyDescriptor> GetAll() => _topologies.Values.ToArray();

    public AzureEnvironmentTopologyDescriptor? TryGet(string environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return null;
        }

        return _topologies.TryGetValue(environmentName, out var topology) ? topology : null;
    }

    public AzureEnvironmentTopologyValidationResult Validate(AzureEnvironmentTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var errors = new List<string>();
        var warnings = new List<string>();

        Require(descriptor.EnvironmentName, nameof(descriptor.EnvironmentName), errors);
        Require(descriptor.DeploymentRing, nameof(descriptor.DeploymentRing), errors);
        Require(descriptor.Region, nameof(descriptor.Region), errors);
        Require(descriptor.ResourceGroupName, nameof(descriptor.ResourceGroupName), errors);
        Require(descriptor.SqlOperationalStoreName, nameof(descriptor.SqlOperationalStoreName), errors);
        Require(descriptor.ArtifactStorageAccountName, nameof(descriptor.ArtifactStorageAccountName), errors);
        Require(descriptor.QueueNamespaceName, nameof(descriptor.QueueNamespaceName), errors);
        Require(descriptor.TelemetryResourceName, nameof(descriptor.TelemetryResourceName), errors);

        if (descriptor.AllowsRealMigrationExecution && descriptor.DeploymentRing.Equals("local", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Local deployment ring allows real migration execution. Confirm this is intentional.");
        }

        if (!descriptor.RequiresManagedIdentity && !descriptor.DeploymentRing.Equals("local", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-local topology does not require managed identity. Confirm this is intentional.");
        }

        return new AzureEnvironmentTopologyValidationResult(errors, warnings);
    }

    private static void Require(string value, string name, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{name} is required.");
        }
    }
}
