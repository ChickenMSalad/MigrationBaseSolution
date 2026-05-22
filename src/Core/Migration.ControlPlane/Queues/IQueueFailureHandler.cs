namespace Migration.ControlPlane.Queues;

public interface IQueueFailureHandler
{
    Task<QueueFailureHandlingResult> HandleFailureAsync(
        QueueFailureArtifactRequest request,
        QueuePoisonHandlingPlan poisonPlan,
        CancellationToken cancellationToken = default);
}

public sealed record QueueFailureHandlingResult(
    bool FailureArtifactWritten,
    string Strategy,
    string? ArtifactObjectKey,
    string RecommendedNextAction,
    IReadOnlyList<string> Warnings);
