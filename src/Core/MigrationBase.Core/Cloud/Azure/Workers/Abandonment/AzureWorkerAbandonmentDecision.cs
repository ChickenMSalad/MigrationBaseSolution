namespace MigrationBase.Core.Cloud.Azure.Workers.Abandonment;

/// <summary>
/// Captures the operational decision made when a worker abandons a work item.
/// </summary>
public sealed class AzureWorkerAbandonmentDecision
{
    public string WorkItemId { get; init; } = string.Empty;

    public string WorkerInstanceId { get; init; } = string.Empty;

    public AzureWorkerAbandonmentReason Reason { get; init; } = AzureWorkerAbandonmentReason.Unknown;

    public bool ShouldRequeue { get; init; }

    public bool ShouldMarkPoison { get; init; }

    public bool RequiresOperatorReview { get; init; }

    public TimeSpan? RequeueDelay { get; init; }

    public string DecisionCode { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public static AzureWorkerAbandonmentDecision Requeue(
        string workItemId,
        string workerInstanceId,
        AzureWorkerAbandonmentReason reason,
        TimeSpan delay,
        string decisionCode)
        => new()
        {
            WorkItemId = workItemId,
            WorkerInstanceId = workerInstanceId,
            Reason = reason,
            ShouldRequeue = true,
            RequeueDelay = delay,
            DecisionCode = decisionCode
        };

    public static AzureWorkerAbandonmentDecision Poison(
        string workItemId,
        string workerInstanceId,
        AzureWorkerAbandonmentReason reason,
        string decisionCode)
        => new()
        {
            WorkItemId = workItemId,
            WorkerInstanceId = workerInstanceId,
            Reason = reason,
            ShouldMarkPoison = true,
            RequiresOperatorReview = true,
            DecisionCode = decisionCode
        };
}
