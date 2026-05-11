namespace Migration.Domain.Models;

public sealed class MigrationResult
{
    public required string WorkItemId { get; init; }
    public bool Success { get; init; }
    public string? TargetAssetId { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
