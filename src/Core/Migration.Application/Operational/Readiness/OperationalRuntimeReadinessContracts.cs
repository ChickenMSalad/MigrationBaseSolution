namespace Migration.Application.Operational.Readiness;

public interface IOperationalRuntimeReadinessService
{
    Task<OperationalRuntimeReadinessReport> GetReadinessAsync(CancellationToken cancellationToken = default);

    Task<OperationalRunReadinessReport> GetRunReadinessAsync(Guid runId, CancellationToken cancellationToken = default);
}

public sealed record OperationalRuntimeReadinessReport(
    string Status,
    bool IsReady,
    DateTimeOffset EvaluatedUtc,
    IReadOnlyList<OperationalRuntimeReadinessCheck> Checks,
    IReadOnlyList<string> BlockingIssues,
    IReadOnlyList<string> Warnings);

public sealed record OperationalRuntimeReadinessCheck(
    string Name,
    string Status,
    bool IsRequired,
    string? Detail);

public sealed record OperationalRunReadinessReport(
    Guid RunId,
    string Status,
    bool CanStart,
    bool CanDispatch,
    bool CanExecute,
    DateTimeOffset EvaluatedUtc,
    OperationalRunReadinessCounts Counts,
    IReadOnlyList<string> BlockingIssues,
    IReadOnlyList<string> Warnings);

public sealed record OperationalRunReadinessCounts(
    int ManifestRowCount,
    int PendingManifestRowCount,
    int WorkItemCount,
    int PendingWorkItemCount,
    int LeasedWorkItemCount,
    int CompletedWorkItemCount,
    int FailedWorkItemCount,
    int RetryableFailedWorkItemCount);
