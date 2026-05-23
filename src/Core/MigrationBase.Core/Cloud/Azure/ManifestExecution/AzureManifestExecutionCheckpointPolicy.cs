namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionCheckpointPolicy
{
    public int RecordEveryItemCount { get; init; } = 100;

    public bool RecordAtBatchCompletion { get; init; } = true;

    public bool RecordAtStepCompletion { get; init; } = true;

    public bool ShouldRecordForProcessedCount(long processedCount)
    {
        if (RecordEveryItemCount <= 0)
        {
            return false;
        }

        return processedCount > 0 && processedCount % RecordEveryItemCount == 0;
    }
}
