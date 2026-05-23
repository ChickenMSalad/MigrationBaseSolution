namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionBatchRunRequest
{
    public required AzureManifestExecutionContext Context { get; init; }

    public required AzureManifestExecutionBatch Batch { get; init; }

    public int AttemptNumber { get; init; } = 1;

    public bool ContinueOnItemFailure { get; init; } = true;
}
