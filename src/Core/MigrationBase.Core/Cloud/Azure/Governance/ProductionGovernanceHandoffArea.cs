namespace MigrationBase.Core.Cloud.Azure.Governance;

public enum ProductionGovernanceHandoffArea
{
    RuntimeTopology = 0,
    WorkerStabilization = 1,
    DeploymentAutomation = 2,
    Observability = 3,
    RealMigrationValidation = 4,
    OperationalGovernance = 5
}
