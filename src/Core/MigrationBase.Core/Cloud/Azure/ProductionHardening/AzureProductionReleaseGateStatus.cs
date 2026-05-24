namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public enum AzureProductionReleaseGateStatus
{
    NotEvaluated = 0,
    Passed = 1,
    PassedWithWarnings = 2,
    Failed = 3,
    Blocked = 4
}
