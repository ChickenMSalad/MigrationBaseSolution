namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public enum AzureWorkerDispatchDeadLetterReason
{
    Unknown = 0,
    MaxAttemptsExceeded = 1,
    PoisonDetected = 2,
    InvalidEnvelope = 3,
    LeaseConflict = 4,
    OperatorRequested = 5
}
