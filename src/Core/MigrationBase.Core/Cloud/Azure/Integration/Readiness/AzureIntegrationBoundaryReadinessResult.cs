namespace MigrationBase.Core.Cloud.Azure.Integration.Readiness;

public sealed class AzureIntegrationBoundaryReadinessResult
{
    public bool IsReady => Findings.All(finding => finding.Level != AzureIntegrationBoundaryReadinessLevel.Blocking);
    public List<AzureIntegrationBoundaryReadinessCheck> Checks { get; } = new();
    public List<AzureIntegrationBoundaryReadinessFinding> Findings { get; } = new();
}
