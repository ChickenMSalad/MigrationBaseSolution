using Migration.Domain.Models;
using Migration.Orchestration.Abstractions;

namespace Migration.Admin.Api.Models;

public sealed record CreateMigrationProjectRequest(string DisplayName, string SourceType, string TargetType, string ManifestType, Dictionary<string, string?>? Settings = null);
public sealed record UpdateMigrationProjectRequest(string? DisplayName = null, string? SourceType = null, string? TargetType = null, string? ManifestType = null, Dictionary<string, string?>? Settings = null);

public sealed record MigrationProjectRecord
{
    public required string ProjectId { get; init; }
    public required string DisplayName { get; init; }
    public required string SourceType { get; init; }
    public required string TargetType { get; init; }
    public required string ManifestType { get; init; }
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string?> Settings { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record CreateRunRequest(string? JobName, string ManifestPath, string MappingProfilePath, bool DryRun = true, int Parallelism = 1, Dictionary<string, string?>? Settings = null);
public sealed record CreatePreflightRequest(string? JobName, string ManifestPath, string MappingProfilePath, Dictionary<string, string?>? Settings = null);

public sealed record MigrationRunControlRecord
{
    public required string RunId { get; init; }
    public required string ProjectId { get; init; }
    public required string JobName { get; init; }
    public required string Status { get; init; }
    public bool DryRun { get; init; }
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedUtc { get; init; }
    public DateTimeOffset? CompletedUtc { get; init; }
    public string? Message { get; init; }
    public MigrationJobDefinition Job { get; init; } = default!;
}

public sealed record RunWorkItemsResponse(string RunId, string JobName, int Count, IReadOnlyList<MigrationWorkItemState> WorkItems);

public static class AdminRunStatuses
{
    public const string Queued = "Queued";
    public const string PreflightQueued = "PreflightQueued";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Canceled = "Canceled";
}
