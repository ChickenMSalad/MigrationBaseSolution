namespace Migration.ControlPlane.Queues;

public sealed record QueueExecutorCoordinatorOptions(
    bool DryRun,
    bool CompleteMessages,
    bool WriteFailureArtifacts,
    int MaxMessages);

public sealed record QueueExecutorCoordinatorResult(
    int ReceivedCount,
    int PlannedCount,
    int ExecutableCount,
    int CompletedCount,
    int FailureCount,
    IReadOnlyList<QueueExecutorMessageResult> Messages,
    IReadOnlyList<string> Warnings);

public sealed record QueueExecutorMessageResult(
    string ProviderMessageId,
    string MessageType,
    string? ProjectId,
    string? RunId,
    string IdempotencyKey,
    bool CanExecute,
    string Action,
    bool Completed,
    bool FailureHandled,
    string? FailureArtifactObjectKey,
    IReadOnlyList<string> Warnings);
