namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Conservative default poison classifier. Infrastructure-specific implementations can replace this later.
/// </summary>
public sealed class AzureWorkerPoisonClassifier : IAzureWorkerPoisonClassifier
{
    public AzureWorkerPoisonClassification Classify(string workItemId, string runId, Exception exception, int attemptNumber)
    {
        if (string.IsNullOrWhiteSpace(workItemId))
        {
            throw new ArgumentException("Work item id is required.", nameof(workItemId));
        }

        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        if (exception is OperationCanceledException)
        {
            return new AzureWorkerPoisonClassification
            {
                Code = "worker.cancelled",
                Reason = "Worker execution was cancelled before completion.",
                Disposition = AzureWorkerPoisonDisposition.Abandon,
                IsReplayEligible = true,
                SuggestedDelay = TimeSpan.FromSeconds(30)
            };
        }

        if (attemptNumber >= 5)
        {
            return new AzureWorkerPoisonClassification
            {
                Code = "worker.max-attempts-exceeded",
                Reason = "The work item exceeded the configured retry threshold.",
                Disposition = AzureWorkerPoisonDisposition.DeadLetter,
                IsOperatorActionRequired = true,
                IsReplayEligible = true
            };
        }

        return new AzureWorkerPoisonClassification
        {
            Code = "worker.transient-failure",
            Reason = exception.Message,
            Disposition = AzureWorkerPoisonDisposition.Retry,
            IsReplayEligible = true,
            SuggestedDelay = TimeSpan.FromSeconds(Math.Min(300, Math.Max(15, attemptNumber * 30)))
        };
    }
}
