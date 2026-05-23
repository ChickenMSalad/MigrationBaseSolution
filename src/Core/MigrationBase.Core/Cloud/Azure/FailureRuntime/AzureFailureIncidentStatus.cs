namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public enum AzureFailureIncidentStatus
{
    Open = 0,
    RetryScheduled = 1,
    ReplayRequested = 2,
    Resolved = 3,
    Suppressed = 4,
    DeadLettered = 5
}
