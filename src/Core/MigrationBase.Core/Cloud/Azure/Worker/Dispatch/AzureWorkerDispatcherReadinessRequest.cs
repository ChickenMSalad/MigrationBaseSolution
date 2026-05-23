namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatcherReadinessRequest
{
    public required string WorkerId { get; init; }

    public bool RequireQueueReader { get; init; } = true;

    public bool RequireQueueWriter { get; init; } = true;

    public bool RequireClaimStore { get; init; } = true;

    public bool RequireCompletionSink { get; init; } = true;

    public bool RequireDeadLetterSink { get; init; } = true;
}
