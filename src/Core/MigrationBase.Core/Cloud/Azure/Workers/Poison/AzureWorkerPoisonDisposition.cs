namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Describes how the runtime should handle work that cannot safely continue.
/// </summary>
public enum AzureWorkerPoisonDisposition
{
    None = 0,
    Retry = 1,
    Abandon = 2,
    Quarantine = 3,
    DeadLetter = 4,
    Escalate = 5
}
