namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatcherReadinessOptions
{
    public const string SectionName = "AzureRuntime:WorkerDispatcherReadiness";

    public bool Enabled { get; set; } = true;

    public bool RequireQueueReader { get; set; } = true;

    public bool RequireQueueWriter { get; set; } = true;

    public bool RequireClaimStore { get; set; } = true;

    public bool RequireCompletionSink { get; set; } = true;

    public bool RequireDeadLetterSink { get; set; } = true;
}
