namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Classifies worker execution failures into retry, abandonment, quarantine, dead-letter, or escalation paths.
/// </summary>
public interface IAzureWorkerPoisonClassifier
{
    AzureWorkerPoisonClassification Classify(string workItemId, string runId, Exception exception, int attemptNumber);
}
