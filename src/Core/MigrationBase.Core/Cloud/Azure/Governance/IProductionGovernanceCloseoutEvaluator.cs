namespace MigrationBase.Core.Cloud.Azure.Governance;

public interface IProductionGovernanceCloseoutEvaluator
{
    ProductionGovernanceCloseoutResult Evaluate(ProductionGovernanceCloseoutManifest manifest);
}
