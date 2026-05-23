namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionScope
{
    public required string RunId { get; init; }

    public required string ManifestId { get; init; }

    public string? SourceSystem { get; init; }

    public string? TargetSystem { get; init; }

    public AzureManifestExecutionMode Mode { get; init; } = AzureManifestExecutionMode.ValidateOnly;
}
