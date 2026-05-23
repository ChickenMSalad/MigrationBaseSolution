namespace MigrationBase.Core.Cloud.Azure.Deployment;

public interface IAzureDeploymentPromotionEvaluator
{
    AzureDeploymentPromotionDecision Evaluate(
        AzureDeploymentPromotionPolicy policy,
        IReadOnlyDictionary<string, string?> evidence);
}
