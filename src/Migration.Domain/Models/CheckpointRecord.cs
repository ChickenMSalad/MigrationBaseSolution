namespace Migration.Domain.Models;

public sealed class CheckpointRecord
{
    public required string JobName { get; init; }
    public required string WorkItemId { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public string? Detail { get; init; }
}
