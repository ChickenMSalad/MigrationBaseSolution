namespace MigrationBase.Core.Cloud.Azure.Validation;

public enum AzureRuntimeValidationGateStatus
{
    NotEvaluated = 0,
    Passed = 1,
    Warning = 2,
    Failed = 3,
    Skipped = 4
}
