namespace Migration.Core.Azure.Topology;

public static class AzureRuntimeTopologyValidation
{
    public static IReadOnlyList<string> Validate(AzureRuntimeTopologyRegistryOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var errors = new List<string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < options.Topologies.Count; i++)
        {
            var topology = options.Topologies[i];
            var prefix = $"Topologies[{i}]";

            if (string.IsNullOrWhiteSpace(topology.Name))
            {
                errors.Add($"{prefix}.Name is required.");
                continue;
            }

            if (!names.Add(topology.Name.Trim()))
            {
                errors.Add($"Duplicate topology name '{topology.Name}'.");
            }

            if (topology.EnvironmentKind == AzureRuntimeEnvironmentKind.Unknown)
            {
                errors.Add($"{prefix}.EnvironmentKind must be specified for '{topology.Name}'.");
            }

            if (topology.DeploymentRing == AzureDeploymentRing.Unknown)
            {
                errors.Add($"{prefix}.DeploymentRing must be specified for '{topology.Name}'.");
            }

            if (topology.EnvironmentKind == AzureRuntimeEnvironmentKind.Production && topology.AllowsDestructiveOperations)
            {
                errors.Add($"Production topology '{topology.Name}' must not allow destructive operations by default.");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.DefaultTopologyName) && !names.Contains(options.DefaultTopologyName.Trim()))
        {
            errors.Add($"Default topology '{options.DefaultTopologyName}' was not found in the topology registry.");
        }

        return errors;
    }
}
