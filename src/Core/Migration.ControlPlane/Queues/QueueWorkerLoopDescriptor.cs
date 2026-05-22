namespace Migration.ControlPlane.Queues;

public sealed record QueueWorkerLoopDescriptor(
    bool Enabled,
    bool DryRun,
    int MaxMessages,
    int PollIntervalSeconds,
    int VisibilityTimeoutSeconds,
    string ReceiveProviderKind,
    string LogicalQueueName,
    bool ReceiveProviderConfigured,
    IReadOnlyList<string> Warnings);
