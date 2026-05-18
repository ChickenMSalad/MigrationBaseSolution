namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorCheckpointItem
{
    public Guid CheckpointId { get; init; }

    public Guid RunId { get; init; }

    public string CheckpointName { get; init; } = string.Empty;

    public string CheckpointValue { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
