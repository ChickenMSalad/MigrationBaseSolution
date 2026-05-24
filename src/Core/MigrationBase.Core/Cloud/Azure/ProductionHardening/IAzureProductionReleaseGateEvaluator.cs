namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public interface IAzureProductionReleaseGateEvaluator
{
    AzureProductionReleaseGateResult Evaluate(AzureProductionReleaseGateRequest request);
}
