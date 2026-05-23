namespace Migration.Core.Azure.Topology;

/// <summary>
/// Describes a named Azure runtime topology for hosts, workers, deployments, and validation tooling.
/// </summary>
public sealed class AzureRuntimeTopologyDescriptor
{
    public string Name { get; set; } = string.Empty;

    public AzureRuntimeEnvironmentKind EnvironmentKind { get; set; } = AzureRuntimeEnvironmentKind.Unknown;

    public AzureDeploymentRing DeploymentRing { get; set; } = AzureDeploymentRing.Unknown;

    public string? AzureRegion { get; set; }

    public string? TenantBoundary { get; set; }

    public string? OperationalStoreProfile { get; set; }

    public string? ArtifactStorageProfile { get; set; }

    public string? QueueProfile { get; set; }

    public bool AllowsDestructiveOperations { get; set; }

    public bool RequiresManagedIdentity { get; set; } = true;

    public AzureRuntimeResourceTopology Resources { get; set; } = new();
}
