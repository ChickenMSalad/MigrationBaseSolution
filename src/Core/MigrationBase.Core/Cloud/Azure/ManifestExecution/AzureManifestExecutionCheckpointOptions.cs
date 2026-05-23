namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionCheckpointOptions
{
    public const string SectionName = "AzureRuntime:ManifestExecutionCheckpoint";

    public bool Enabled { get; set; } = true;

    public int RecordEveryItemCount { get; set; } = 100;

    public bool RecordAtBatchCompletion { get; set; } = true;

    public bool RecordAtStepCompletion { get; set; } = true;

    public bool UseInMemoryCheckpointStore { get; set; } = true;
}
