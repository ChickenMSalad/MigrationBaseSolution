namespace MigrationBase.Core.Cloud.Azure.Readiness;

public enum CloudOperationalizationCloseoutStatus
{
    NotStarted = 0,
    InProgress = 1,
    ReadyForImplementation = 2,
    Blocked = 3,
    Completed = 4
}
