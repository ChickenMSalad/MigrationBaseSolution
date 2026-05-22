namespace Migration.ControlPlane.Queues;

public sealed record QueuePoisonHandlingOptions(
    int MaxAttempts,
    string PoisonStrategy,
    string? DeadLetterQueueName,
    bool PersistFailureArtifact,
    string FailureArtifactKind);

public sealed record QueuePoisonHandlingPlan(
    string ProviderKind,
    string LogicalQueueName,
    int MaxAttempts,
    string PoisonStrategy,
    string? DeadLetterQueueName,
    bool NativeDeadLetterSupported,
    bool PersistFailureArtifact,
    string FailureArtifactKind,
    IReadOnlyList<string> Warnings);

public static class QueuePoisonStrategies
{
    public const string None = "none";
    public const string Abandon = "abandon";
    public const string Delete = "delete";
    public const string DeadLetterQueue = "deadLetterQueue";
    public const string FailureArtifact = "failureArtifact";
}
