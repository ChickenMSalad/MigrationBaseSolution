using Migration.Domain.Models;

namespace Migration.Orchestration.Preflight;

public sealed record PreflightRequest(
    string? ProjectId,
    MigrationJobDefinition Job,
    int MaxRows = 250,
    bool ValidateSourceSample = false,
    int SourceSampleSize = 0);

public sealed record PreflightResult
{
    public required string PreflightId { get; init; }
    public string? ProjectId { get; init; }
    public required string JobName { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset StartedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CompletedUtc { get; init; } = DateTimeOffset.UtcNow;
    public PreflightSummary Summary { get; init; } = new();
    public List<PreflightIssue> Issues { get; init; } = new();
    public Dictionary<string, object?> Details { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record PreflightSummary
{
    public int TotalRows { get; init; }
    public int CheckedRows { get; init; }
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public int InfoCount { get; init; }
    public bool Passed => ErrorCount == 0;
}

public sealed record PreflightIssue
{
    public required string Severity { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? RowId { get; init; }
    public string? Field { get; init; }
    public string? SourceAssetId { get; init; }
}

public static class PreflightStatuses
{
    public const string Passed = "Passed";
    public const string Warning = "Warning";
    public const string Failed = "Failed";
}

public static class PreflightSeverities
{
    public const string Error = "Error";
    public const string Warning = "Warning";
    public const string Info = "Info";
}
