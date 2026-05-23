namespace MigrationBase.Core.Cloud.Azure.Drift;

public sealed class AzureEnvironmentDriftReport
{
    public string EnvironmentName { get; set; } = string.Empty;
    public string DeploymentRing { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public IList<AzureEnvironmentDriftDescriptor> Findings { get; set; } = new List<AzureEnvironmentDriftDescriptor>();

    public bool HasBlockingDrift => Findings.Any(f =>
        f.Severity == AzureEnvironmentDriftSeverity.Blocking ||
        f.Severity == AzureEnvironmentDriftSeverity.Critical);
}
