namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionCheckpointResult
{
    private AzureManifestExecutionCheckpointResult(
        bool recorded,
        AzureManifestExecutionCheckpoint? checkpoint,
        string? reason)
    {
        Recorded = recorded;
        Checkpoint = checkpoint;
        Reason = reason;
    }

    public bool Recorded { get; }

    public AzureManifestExecutionCheckpoint? Checkpoint { get; }

    public string? Reason { get; }

    public static AzureManifestExecutionCheckpointResult Success(
        AzureManifestExecutionCheckpoint checkpoint)
    {
        return new AzureManifestExecutionCheckpointResult(true, checkpoint, null);
    }

    public static AzureManifestExecutionCheckpointResult Rejected(string reason)
    {
        return new AzureManifestExecutionCheckpointResult(false, null, reason);
    }
}
