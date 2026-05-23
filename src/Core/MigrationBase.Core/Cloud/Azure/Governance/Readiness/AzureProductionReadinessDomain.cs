namespace MigrationBase.Core.Cloud.Azure.Governance.Readiness;

public enum AzureProductionReadinessDomain
{
    Unknown = 0,
    RuntimeTopology = 1,
    WorkerStability = 2,
    DeploymentAutomation = 3,
    Observability = 4,
    MigrationValidation = 5,
    OperationalGovernance = 6,
    SecurityAndIdentity = 7,
    DataAndSqlDurability = 8
}
