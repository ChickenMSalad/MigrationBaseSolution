namespace MigrationBase.Core.Cloud.Azure.Workers.Retry;

public static class AzureWorkerRetryFailureDispositions
{
    public const string Abandon = "abandon";
    public const string DeadLetter = "dead-letter";
    public const string FailWorkItem = "fail-work-item";
    public const string Escalate = "escalate";
}
