namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Conservative default classifier. It only marks work as poison after configured attempt thresholds are crossed.
/// </summary>
public sealed class AzureWorkerPoisonWorkClassifier : IAzureWorkerPoisonWorkClassifier
{
    public AzureWorkerPoisonWorkClassification Classify(AzureWorkerPoisonWorkPolicy policy, int consecutiveFailures, int totalAttempts, string? failureCode)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var isPoison = consecutiveFailures >= policy.MaxConsecutiveFailures || totalAttempts >= policy.MaxTotalAttempts;
        if (!isPoison)
        {
            return new AzureWorkerPoisonWorkClassification
            {
                ClassificationCode = "retryable",
                DisplayName = "Retryable failure",
                Description = "The failure has not crossed poison-work thresholds.",
                DefaultAction = AzureWorkerPoisonWorkAction.None,
                RequiresManualDisposition = false
            };
        }

        return new AzureWorkerPoisonWorkClassification
        {
            ClassificationCode = string.IsNullOrWhiteSpace(failureCode) ? "poison-work" : failureCode,
            DisplayName = "Poison work",
            Description = "The work item crossed configured failure thresholds and should leave the normal retry path.",
            DefaultAction = policy.DefaultPoisonAction,
            BlocksReplayAdmission = true,
            RequiresManualDisposition = policy.DefaultPoisonAction == AzureWorkerPoisonWorkAction.RequireOperatorReview
        };
    }
}
