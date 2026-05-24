namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public enum AzureEndToEndValidationStatus
{
    NotRun = 0,
    Passed = 1,
    PassedWithWarnings = 2,
    Failed = 3,
    Skipped = 4
}
