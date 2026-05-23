namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public enum AzureFailureClassification
{
    Unknown = 0,
    Transient = 1,
    Permanent = 2,
    Poison = 3,
    OperatorActionRequired = 4,
    ReplayEligible = 5
}
