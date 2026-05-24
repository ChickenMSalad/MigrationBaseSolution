namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public enum AzureProductionReadinessItemStatus
{
    NotEvaluated = 0,
    Passed = 1,
    Warning = 2,
    Failed = 3,
    Waived = 4
}
