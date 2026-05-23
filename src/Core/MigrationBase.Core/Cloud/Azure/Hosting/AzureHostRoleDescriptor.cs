namespace MigrationBase.Core.Cloud.Azure.Hosting;

/// <summary>
/// Describes how a deployable application participates in the Azure runtime topology.
/// </summary>
public sealed class AzureHostRoleDescriptor
{
    public string HostName { get; set; } = string.Empty;

    public AzureHostRoleKind RoleKind { get; set; } = AzureHostRoleKind.Unknown;

    public string DeploymentUnit { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsInteractive { get; set; }

    public bool ProcessesMigrationWork { get; set; }

    public bool PublishesOperationalEvents { get; set; }

    public bool RequiresManagedIdentity { get; set; } = true;

    public List<AzureHostWorkloadCapability> Capabilities { get; set; } = new();
}
