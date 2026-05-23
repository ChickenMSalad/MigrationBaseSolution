namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionContextRequest
{
    public required AzureManifestExecutionPlan Plan { get; init; }

    public AzureManifestExecutionStatus InitialStatus { get; init; } =
        AzureManifestExecutionStatus.Preparing;

    public string? RequestedBy { get; init; }
}
