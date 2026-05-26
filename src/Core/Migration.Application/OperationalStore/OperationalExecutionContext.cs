namespace Migration.Application.OperationalStore;

public sealed class OperationalExecutionContext
{
    public OperationalExecutionContext(
        Guid runId,
        long manifestRecordId,
        long workItemId,
        string sourceId,
        string? sourcePath,
        string? sourceName)
    {
        RunId = runId;
        ManifestRecordId = manifestRecordId;
        WorkItemId = workItemId;
        SourceId = sourceId;
        SourcePath = sourcePath;
        SourceName = sourceName;
    }

    public Guid RunId { get; }

    public long ManifestRecordId { get; }

    public long WorkItemId { get; }

    public string SourceId { get; }

    public string? SourcePath { get; }

    public string? SourceName { get; }
}
