namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionCompletionResult
{
    private AzureManifestExecutionCompletionResult(
        bool recorded,
        AzureManifestExecutionCompletionRecord? record,
        string? reason)
    {
        Recorded = recorded;
        Record = record;
        Reason = reason;
    }

    public bool Recorded { get; }

    public AzureManifestExecutionCompletionRecord? Record { get; }

    public string? Reason { get; }

    public static AzureManifestExecutionCompletionResult Success(
        AzureManifestExecutionCompletionRecord record)
    {
        return new AzureManifestExecutionCompletionResult(true, record, null);
    }

    public static AzureManifestExecutionCompletionResult Rejected(string reason)
    {
        return new AzureManifestExecutionCompletionResult(false, null, reason);
    }
}
