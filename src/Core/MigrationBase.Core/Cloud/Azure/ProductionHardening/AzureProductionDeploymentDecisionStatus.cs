namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public enum AzureProductionDeploymentDecisionStatus
{
    NotEvaluated = 0,
    Approved = 1,
    ApprovedWithWarnings = 2,
    Rejected = 3,
    Blocked = 4
}
