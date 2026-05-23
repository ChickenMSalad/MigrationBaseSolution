namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchCompletionOptions
{
    public const string SectionName = "AzureRuntime:WorkerDispatchCompletion";

    public bool Enabled { get; set; } = true;

    public bool ReleaseClaimOnCompletion { get; set; } = true;

    public bool RecordAbandonedCompletions { get; set; } = true;

    public bool RecordPoisonedCompletions { get; set; } = true;

    public bool UseInMemoryCompletionSink { get; set; } = true;
}
