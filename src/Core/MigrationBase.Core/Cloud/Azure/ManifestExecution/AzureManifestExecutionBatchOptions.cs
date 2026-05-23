namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionBatchOptions
{
    public const string SectionName = "AzureRuntime:ManifestExecutionBatch";

    public bool Enabled { get; set; } = true;

    public int DefaultBatchSize { get; set; } = 100;

    public int MaxBatchSize { get; set; } = 1000;

    public bool RequireCursorCheckpointing { get; set; } = true;

    public bool UseInMemoryBatchProvider { get; set; } = true;
}
