using Migration.Domain.Models;

namespace Migration.Orchestration.Abstractions;

public interface IMigrationJobRunner
{
    Task<MigrationRunSummary> RunAsync(MigrationJobDefinition job, CancellationToken cancellationToken = default);
}

public sealed record MigrationRunSummary(
    string RunId,
    string JobName,
    int TotalWorkItems,
    int Succeeded,
    int Failed,
    int Skipped,
    TimeSpan Elapsed,
    IReadOnlyList<MigrationResult> Results);
