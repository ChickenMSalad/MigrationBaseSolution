namespace MigrationBase.Core.Cloud.Azure.Worker.Runtime;

public sealed class AzureWorkerExecutionOutcomeClassifier : IAzureWorkerExecutionOutcomeClassifier
{
    public AzureWorkerExecutionOutcome ClassifySuccess(string workItemId, string? message = null)
    {
        return AzureWorkerExecutionOutcome.Completed(workItemId, message);
    }

    public AzureWorkerExecutionOutcome ClassifyFailure(
        string workItemId,
        Exception exception,
        int attemptNumber,
        AzureWorkerRetryPolicy retryPolicy)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(retryPolicy);

        if (exception is OperationCanceledException)
        {
            return ClassifyCancellation(workItemId, attemptNumber, exception.Message);
        }

        var retryAfter = retryPolicy.CalculateDelay(attemptNumber + 1);
        return AzureWorkerExecutionOutcome.RetryableFailure(
            workItemId,
            attemptNumber,
            retryPolicy.MaxAttempts,
            retryAfter,
            exception.GetType().Name,
            exception.Message);
    }

    public AzureWorkerExecutionOutcome ClassifyCancellation(string workItemId, int attemptNumber, string? message = null)
    {
        return new AzureWorkerExecutionOutcome
        {
            WorkItemId = workItemId,
            Kind = AzureWorkerExecutionOutcomeKind.Cancelled,
            RetryDisposition = AzureWorkerRetryDisposition.Abandon,
            AttemptNumber = attemptNumber,
            Message = message
        };
    }
}
