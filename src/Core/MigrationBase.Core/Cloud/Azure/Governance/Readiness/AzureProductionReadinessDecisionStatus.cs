namespace MigrationBase.Core.Cloud.Azure.Governance.Readiness;

public enum AzureProductionReadinessDecisionStatus
{
    Pending = 0,
    Approved = 1,
    ApprovedWithRiskAcceptance = 2,
    Rejected = 3,
    Superseded = 4
}
