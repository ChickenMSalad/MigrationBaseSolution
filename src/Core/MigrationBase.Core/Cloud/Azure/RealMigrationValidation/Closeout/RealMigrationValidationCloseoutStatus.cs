namespace MigrationBase.Core.Cloud.Azure.RealMigrationValidation.Closeout;

public enum RealMigrationValidationCloseoutStatus
{
    NotStarted = 0,
    InProgress = 1,
    Blocked = 2,
    ReadyForOperationalRun = 3,
    ReadyForProductionPilot = 4,
    Complete = 5
}
