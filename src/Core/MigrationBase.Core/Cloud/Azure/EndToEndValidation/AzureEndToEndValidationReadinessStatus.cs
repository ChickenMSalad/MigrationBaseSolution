namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public enum AzureEndToEndValidationReadinessStatus
{
    Ready = 0,
    Degraded = 1,
    NotReady = 2
}
