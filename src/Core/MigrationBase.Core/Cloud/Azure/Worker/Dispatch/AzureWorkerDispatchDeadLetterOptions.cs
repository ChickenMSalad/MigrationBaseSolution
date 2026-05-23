namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchDeadLetterOptions
{
    public const string SectionName = "AzureRuntime:WorkerDispatchDeadLetter";

    public bool Enabled { get; set; } = true;

    public string DeadLetterQueueName { get; set; } = "migration-work-items-deadletter";

    public bool ReleaseClaimOnDeadLetter { get; set; } = true;

    public bool UseInMemoryDeadLetterSink { get; set; } = true;
}
