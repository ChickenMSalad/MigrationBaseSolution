using Migration.Application.Operational.WorkItems;

namespace Migration.Application.Operational.Runs;

public interface IOperationalRunCoordinator
{
    Task<OperationalRunCoordinatorRunSnapshot?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<OperationalRunStartResult> StartRunAsync(StartOperationalRunRequest request, CancellationToken cancellationToken = default);

    Task<OperationalRunCancellationResult> RequestCancellationAsync(RequestOperationalRunCancellation request, CancellationToken cancellationToken = default);

    Task<OperationalRunCompletionEvaluationResult> EvaluateCompletionAsync(Guid runId, CancellationToken cancellationToken = default);
}

public sealed record StartOperationalRunRequest(
    Guid RunId,
    string CoordinatorId,
    int BatchSize,
    string WorkItemType,
    string? PartitionKey,
    int Priority,
    string? PayloadTemplateJson);

public sealed record RequestOperationalRunCancellation(
    Guid RunId,
    string RequestedBy,
    string Reason);

public sealed record OperationalRunCoordinatorRunSnapshot(
    Guid RunId,
    Guid ProjectId,
    string RunKey,
    string Status,
    string? StatusReason,
    string? CoordinatorOwner,
    DateTimeOffset? CoordinationLeaseExpiresUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? RequestedCancellationUtc,
    string? CancellationReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record OperationalRunStartResult(
    Guid RunId,
    string Status,
    int EnqueuedWorkItemCount,
    int ManifestRowsSelected,
    DateTimeOffset StartedAtUtc,
    string CoordinatorId);

public sealed record OperationalRunCancellationResult(
    Guid RunId,
    string Status,
    string RequestedBy,
    string Reason,
    DateTimeOffset RequestedUtc);

public sealed record OperationalRunCompletionEvaluationResult(
    Guid RunId,
    string PreviousStatus,
    string CurrentStatus,
    bool IsTerminal,
    OperationalWorkItemRunSummary WorkItemSummary,
    DateTimeOffset EvaluatedUtc,
    string? Message);
