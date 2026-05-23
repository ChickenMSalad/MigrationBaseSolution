namespace MigrationBase.Core.Cloud.Azure.Worker.Runtime;

public enum AzureWorkerExecutionOutcomeKind
{
    Unknown = 0,
    Completed = 1,
    RetryableFailure = 2,
    NonRetryableFailure = 3,
    Cancelled = 4,
    Abandoned = 5,
    Poisoned = 6
}
