namespace MigrationBase.Core.Cloud.Azure.Governance;

public enum AzureProductionReadinessGateCategory
{
    Unknown = 0,
    RuntimeTopology = 1,
    WorkerStability = 2,
    DeploymentAutomation = 3,
    Observability = 4,
    RealMigrationValidation = 5,
    OperationalGovernance = 6
}
