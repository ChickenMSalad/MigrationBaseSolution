namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Describes the action the worker runtime should take when work is classified as poison.
/// </summary>
public enum AzureWorkerPoisonWorkAction
{
    None = 0,
    Quarantine = 1,
    DeadLetter = 2,
    SuspendRun = 3,
    RequireOperatorReview = 4
}
