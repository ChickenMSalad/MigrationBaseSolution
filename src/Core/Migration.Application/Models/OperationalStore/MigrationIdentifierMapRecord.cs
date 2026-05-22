namespace Migration.Application.Models.OperationalStore;

public sealed class MigrationIdentifierMapRecord
{
    public Guid IdentifierMapId { get; init; }

    public Guid RunId { get; init; }

    public Guid ManifestRecordId { get; init; }

    public string SourceId { get; init; } = string.Empty;

    public string TargetId { get; init; } = string.Empty;

    public string? TargetPath { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
