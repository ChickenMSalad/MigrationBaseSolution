namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public interface IAzureProductionRollbackEvaluator
{
    AzureProductionRollbackDecision Evaluate(AzureProductionRollbackRequest request);
}
