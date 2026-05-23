namespace MigrationBase.Core.Cloud.Azure.Governance;

public enum ProductionGovernanceCloseoutStatus
{
    NotStarted = 0,
    InProgress = 1,
    Blocked = 2,
    ReadyForOperationalHandoff = 3,
    Accepted = 4
}
