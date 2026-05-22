namespace Migration.ControlPlane.Queues;

public sealed record QueueExecutionObservabilitySnapshot(
    DateTimeOffset GeneratedUtc,
    string ProviderKind,
    string QueueName,
    bool ReceiveProviderConfigured,
    bool WorkerLoopEnabled,
    bool WorkerLoopDryRun,
    bool CoordinatorDryRun,
    bool CompleteMessages,
    int MaxMessages,
    IReadOnlyList<string> SupportedMessageTypes,
    IReadOnlyList<string> Warnings);
