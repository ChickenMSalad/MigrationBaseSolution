namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public enum AzureProductionRollbackTrigger
{
    Unknown = 0,
    ReleaseGateFailure = 1,
    EndToEndValidationFailure = 2,
    OperatorRequested = 3,
    ErrorRateExceeded = 4,
    HealthSignalDegraded = 5
}
