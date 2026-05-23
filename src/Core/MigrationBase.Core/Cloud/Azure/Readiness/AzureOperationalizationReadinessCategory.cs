namespace MigrationBase.Core.Cloud.Azure.Readiness;

public enum AzureOperationalizationReadinessCategory
{
    RuntimeTopology = 0,
    WorkerStabilization = 1,
    DeploymentAutomation = 2,
    Observability = 3,
    RealMigrationValidation = 4,
    ProductionGovernance = 5
}
