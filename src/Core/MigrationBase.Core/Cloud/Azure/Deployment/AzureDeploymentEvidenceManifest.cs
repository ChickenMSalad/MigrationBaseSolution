namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Captures the evidence set used to prove an Azure deployment target is ready for migration execution.
/// </summary>
public sealed record AzureDeploymentEvidenceManifest
{
    public string EnvironmentName { get; init; } = string.Empty;

    public string DeploymentRing { get; init; } = string.Empty;

    public string TargetName { get; init; } = string.Empty;

    public string TargetKind { get; init; } = string.Empty;

    public string CapturedBy { get; init; } = string.Empty;

    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureDeploymentEvidenceItem> EvidenceItems { get; init; } = Array.Empty<AzureDeploymentEvidenceItem>();

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
