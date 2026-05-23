namespace MigrationBase.Core.Cloud.Azure.Worker.Runtime;

public interface IAzureWorkerExecutionOutcomeClassifier
{
    AzureWorkerExecutionOutcome ClassifySuccess(string workItemId, string? message = null);

    AzureWorkerExecutionOutcome ClassifyFailure(
        string workItemId,
        Exception exception,
        int attemptNumber,
        AzureWorkerRetryPolicy retryPolicy);

    AzureWorkerExecutionOutcome ClassifyCancellation(string workItemId, int attemptNumber, string? message = null);
}
