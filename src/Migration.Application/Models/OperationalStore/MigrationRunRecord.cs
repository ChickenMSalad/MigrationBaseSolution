namespace Migration.Application.Models.OperationalStore;

public sealed class MigrationRunRecord
{
    public Guid RunId { get; init; }

    public string SourceSystem { get; init; } = string.Empty;

    public string TargetSystem { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public DateTimeOffset? FailedAt { get; init; }

    public string? FailureReason { get; init; }
}
