namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureItem
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

    public string RunStatus { get; init; } = string.Empty;

    public string SourceSystem { get; init; } = string.Empty;

    public string TargetSystem { get; init; } = string.Empty;

    public string? WorkItemStatus { get; init; }
}
