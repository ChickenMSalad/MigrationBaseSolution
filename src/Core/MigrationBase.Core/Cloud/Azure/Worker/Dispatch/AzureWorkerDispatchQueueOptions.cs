using System;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchQueueOptions
{
    public const string SectionName = "AzureRuntime:WorkerDispatchQueue";

    public bool Enabled { get; set; } = true;

    public string QueueName { get; set; } = "migration-work-items";

    public int MaxReadBatchSize { get; set; } = 8;

    public TimeSpan EmptyQueueDelay { get; set; } = TimeSpan.FromSeconds(5);

    public bool UseInMemoryQueue { get; set; } = true;
}
