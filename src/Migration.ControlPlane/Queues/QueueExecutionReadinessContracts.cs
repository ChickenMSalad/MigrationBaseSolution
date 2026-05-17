namespace Migration.ControlPlane.Queues;

public sealed record QueueExecutionReadinessSnapshot(
    DateTimeOffset GeneratedUtc,
    bool IsReadyForDryRun,
    bool IsReadyForLiveExecution,
    QueueDispatchProviderDescriptor DispatchProvider,
    QueueReceiveProviderDescriptor ReceiveProvider,
    QueueWorkerLoopDescriptor WorkerLoop,
    QueuePoisonHandlingPlan PoisonHandling,
    QueueExecutionObservabilitySnapshot Observability,
    IReadOnlyList<string> BlockingIssues,
    IReadOnlyList<string> Warnings);
