namespace MigrationBase.Core.Cloud.Azure.ExecutionIsolation;

public sealed class AzureExecutionIsolationProfile
{
    public string ProfileName { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public string DeploymentRing { get; init; } = string.Empty;
    public string IsolationMode { get; init; } = "SharedInfrastructure";
    public IReadOnlyList<AzureExecutionIsolationBoundary> Boundaries { get; init; } = Array.Empty<AzureExecutionIsolationBoundary>();
    public IReadOnlyList<string> RequiredEvidenceKeys { get; init; } = Array.Empty<string>();
}
