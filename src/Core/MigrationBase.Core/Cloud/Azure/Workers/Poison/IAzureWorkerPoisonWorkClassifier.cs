namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Classifies failed work into retryable, quarantined, dead-letter, or operator-review paths.
/// </summary>
public interface IAzureWorkerPoisonWorkClassifier
{
    AzureWorkerPoisonWorkClassification Classify(AzureWorkerPoisonWorkPolicy policy, int consecutiveFailures, int totalAttempts, string? failureCode);
}
