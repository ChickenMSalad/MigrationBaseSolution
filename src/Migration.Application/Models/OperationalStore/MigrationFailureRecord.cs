namespace Migration.Application.Models.OperationalStore;

public sealed class MigrationFailureRecord
{
    public Guid FailureId { get; init; }

    public Guid RunId { get; init; }

    public Guid? ManifestRecordId { get; init; }

    public Guid? WorkItemId { get; init; }

    public string FailureType { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? Details { get; init; }

    public bool IsRetriable { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
