namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Groups Azure deployment targets for one named MigrationBase runtime environment.
/// </summary>
public sealed class AzureDeploymentProfile
{
    public string Name { get; init; } = string.Empty;

    public string EnvironmentName { get; init; } = string.Empty;

    public string SubscriptionName { get; init; } = string.Empty;

    public string TenantName { get; init; } = string.Empty;

    public string DefaultRegion { get; init; } = string.Empty;

    public IReadOnlyList<AzureDeploymentTargetDescriptor> Targets { get; init; } = Array.Empty<AzureDeploymentTargetDescriptor>();
}
