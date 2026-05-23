namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionBatchResult
{
    public required AzureManifestExecutionBatch Batch { get; init; }

    public string? NextCursor { get; init; }

    public bool IsEndOfManifest { get; init; }
}
