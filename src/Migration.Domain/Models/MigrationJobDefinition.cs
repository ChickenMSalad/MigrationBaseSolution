namespace Migration.Domain.Models;

public sealed class MigrationJobDefinition
{
    public required string JobName { get; init; }
    public required string SourceType { get; init; }
    public required string TargetType { get; init; }
    public required string ManifestType { get; init; }
    public required string MappingProfilePath { get; init; }
    public string? ManifestPath { get; init; }
    public string? ConnectionString { get; init; }
    public string? QueryText { get; init; }
    public Dictionary<string, string?> Settings { get; init; } = new();
    public bool DryRun { get; init; }
    public int Parallelism { get; init; } = 1;
}
