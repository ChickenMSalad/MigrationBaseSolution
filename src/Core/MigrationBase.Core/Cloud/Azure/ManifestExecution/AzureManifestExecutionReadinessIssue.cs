namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionReadinessIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? Component { get; init; }
}
