namespace MigrationBase.Core.Cloud.Azure.Drift;

public sealed class AzureEnvironmentDriftDescriptor
{
    public string EnvironmentName { get; set; } = string.Empty;
    public string DeploymentRing { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string DriftCategory { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public AzureEnvironmentDriftSeverity Severity { get; set; } = AzureEnvironmentDriftSeverity.Warning;
    public AzureEnvironmentDriftStatus Status { get; set; } = AzureEnvironmentDriftStatus.Unknown;
    public string RemediationHint { get; set; } = string.Empty;
}
