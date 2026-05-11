using Migration.Domain.Models;

namespace Migration.Orchestration.Abstractions;

public interface IMigrationExecutionStateStore
{
    Task StartRunAsync(MigrationRunRecord run, CancellationToken cancellationToken = default);
    Task CompleteRunAsync(MigrationRunRecord run, CancellationToken cancellationToken = default);
    Task SaveWorkItemAsync(MigrationWorkItemState state, CancellationToken cancellationToken = default);
    Task<MigrationWorkItemState?> GetWorkItemAsync(string jobName, string workItemId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional maintenance surface used by local consoles and future APIs.
/// Durable stores such as Azure Table/Cosmos/SQL should implement this in addition to
/// IMigrationExecutionStateStore so state can be inspected and reset without deleting files by hand.
/// </summary>
public interface IMigrationExecutionStateMaintenance
{
    Task<IReadOnlyList<MigrationWorkItemState>> ListWorkItemsAsync(string jobName, CancellationToken cancellationToken = default);
    Task ResetJobAsync(string jobName, CancellationToken cancellationToken = default);
}

public sealed class MigrationRunRecord
{
    public required string RunId { get; init; }
    public required string JobName { get; init; }
    public required string SourceType { get; init; }
    public required string TargetType { get; init; }
    public bool DryRun { get; init; }
    public DateTimeOffset StartedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedUtc { get; init; }
    public string Status { get; init; } = "Running";
    public int TotalWorkItems { get; init; }
    public int Succeeded { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
    public int ValidationFailed { get; init; }
}

public sealed class MigrationWorkItemState
{
    public required string RunId { get; init; }
    public required string JobName { get; init; }
    public required string WorkItemId { get; init; }
    public required string Status { get; init; }
    public bool DryRun { get; init; }
    public DateTimeOffset? StartedUtc { get; init; }
    public DateTimeOffset? CompletedUtc { get; init; }
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public string? SourceAssetId { get; init; }
    public string? TargetAssetId { get; init; }
    public string? Message { get; init; }
    public string? LastError { get; init; }
    public string? Checksum { get; init; }
    public int AttemptCount { get; init; }
    public Dictionary<string, string?> Properties { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Only a real write success is terminal for resume. Dry-run success must never cause
    /// a later non-dry run to skip the item.
    /// </summary>
    public bool IsTerminalSuccess =>
        !DryRun && string.Equals(Status, MigrationWorkItemStatuses.Succeeded, StringComparison.OrdinalIgnoreCase);
}

public static class MigrationWorkItemStatuses
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string SkippedAlreadySucceeded = "SkippedAlreadySucceeded";
    public const string ValidationFailed = "ValidationFailed";
    public const string SourceFailed = "SourceFailed";
    public const string TargetFailed = "TargetFailed";
    public const string Succeeded = "Succeeded";
    public const string DryRunSucceeded = "DryRunSucceeded";
    public const string Canceled = "Canceled";
}
