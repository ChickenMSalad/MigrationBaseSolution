namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public enum AzureProductionRollbackDecisionStatus
{
    NotEvaluated = 0,
    Continue = 1,
    RollbackRecommended = 2,
    RollbackRequired = 3,
    Blocked = 4
}
