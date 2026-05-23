namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Describes a single evidence-backed gate that must pass before a runtime environment can be promoted.
/// </summary>
public sealed class AzureDeploymentPromotionGate
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Required { get; set; } = true;

    public string EvidenceKey { get; set; } = string.Empty;

    public string ExpectedValue { get; set; } = string.Empty;
}
