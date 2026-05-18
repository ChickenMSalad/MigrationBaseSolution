namespace Migration.Application.OperationalStore;

public sealed class OperationalExecutionContext
{
    public OperationalExecutionContext(
        Guid runId,
        Guid manifestRecordId,
        Guid workItemId,
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

    public Guid ManifestRecordId { get; }

    public Guid WorkItemId { get; }

    public string SourceId { get; }

    public string? SourcePath { get; }

    public string? SourceName { get; }
}
