namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Describes the intended Azure deployment target for a runtime component without requiring Azure SDK references.
/// </summary>
public sealed class AzureDeploymentTargetDescriptor
{
    public string Name { get; init; } = string.Empty;

    public string EnvironmentName { get; init; } = string.Empty;

    public string HostRole { get; init; } = string.Empty;

    public AzureDeploymentTargetKind Kind { get; init; } = AzureDeploymentTargetKind.Unknown;

    public string ResourceGroupName { get; init; } = string.Empty;

    public string ResourceName { get; init; } = string.Empty;

    public string Region { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public bool RequiresManagedIdentity { get; init; } = true;

    public bool RequiresPrivateNetworkAccess { get; init; }

    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
