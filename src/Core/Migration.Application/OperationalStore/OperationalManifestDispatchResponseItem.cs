namespace Migration.Application.OperationalStore;

public sealed class OperationalManifestDispatchResponseItem
{
    public long ManifestRecordId { get; init; }

    public long? WorkItemId { get; init; }

    public string SourceId { get; init; } = string.Empty;

    public string? SourcePath { get; init; }

    public string? SourceName { get; init; }

    public bool QueueMessagePublished { get; init; }
}
