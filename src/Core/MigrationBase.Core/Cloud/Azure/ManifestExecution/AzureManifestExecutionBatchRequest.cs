namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionBatchRequest
{
    public required AzureManifestExecutionContext Context { get; init; }

    public string? Cursor { get; init; }

    public int MaxItems { get; init; } = 100;
}
