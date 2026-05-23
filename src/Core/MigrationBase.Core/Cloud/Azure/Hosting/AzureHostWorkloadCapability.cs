namespace MigrationBase.Core.Cloud.Azure.Hosting;

/// <summary>
/// Describes a runtime capability exposed by a host role.
/// </summary>
public sealed class AzureHostWorkloadCapability
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool RequiresSqlOperationalStore { get; set; }

    public bool RequiresQueueTopology { get; set; }

    public bool RequiresArtifactStorage { get; set; }

    public bool RequiresOperatorAccess { get; set; }
}
