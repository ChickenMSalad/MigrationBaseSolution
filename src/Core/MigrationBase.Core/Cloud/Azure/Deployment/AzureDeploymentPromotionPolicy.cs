namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Describes the minimum operational evidence required to promote one Azure runtime environment to another.
/// </summary>
public sealed class AzureDeploymentPromotionPolicy
{
    public string Name { get; set; } = string.Empty;

    public string SourceEnvironment { get; set; } = string.Empty;

    public string TargetEnvironment { get; set; } = string.Empty;

    public string DeploymentRing { get; set; } = string.Empty;

    public bool RequireManualApproval { get; set; } = true;

    public IList<AzureDeploymentPromotionGate> Gates { get; set; } = new List<AzureDeploymentPromotionGate>();
}
