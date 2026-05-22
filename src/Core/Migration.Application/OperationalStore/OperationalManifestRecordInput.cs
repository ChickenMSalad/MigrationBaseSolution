namespace Migration.Application.OperationalStore;

public sealed class OperationalManifestRecordInput
{
    public long SequenceNumber { get; init; }

    public string SourceId { get; init; } = string.Empty;

    public string? SourcePath { get; init; }

    public string? SourceName { get; init; }

    public string? ContentType { get; init; }

    public long? ContentLength { get; init; }
}
