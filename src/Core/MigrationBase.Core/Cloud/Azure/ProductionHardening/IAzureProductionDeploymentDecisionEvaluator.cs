namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public interface IAzureProductionDeploymentDecisionEvaluator
{
    AzureProductionDeploymentDecision Evaluate(
        AzureProductionDeploymentDecisionRequest request);
}
