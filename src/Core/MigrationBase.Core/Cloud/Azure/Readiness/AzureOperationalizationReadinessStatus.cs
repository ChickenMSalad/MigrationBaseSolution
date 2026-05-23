namespace MigrationBase.Core.Cloud.Azure.Readiness;

public enum AzureOperationalizationReadinessStatus
{
    NotStarted = 0,
    Defined = 1,
    Validated = 2,
    Blocked = 3,
    Accepted = 4
}
