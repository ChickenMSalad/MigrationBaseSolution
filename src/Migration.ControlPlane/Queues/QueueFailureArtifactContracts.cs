namespace Migration.ControlPlane.Queues;

public sealed record QueueFailureArtifactRequest(
    string WorkspaceId,
    string ProjectId,
    string RunId,
    string MessageType,
    string IdempotencyKey,
    string FailureReason,
    string ExceptionType,
    string ExceptionMessage,
    int Attempt,
    DateTimeOffset FailedUtc);

public sealed record QueueFailureArtifactDescriptor(
    string WorkspaceId,
    string ArtifactKind,
    string ArtifactId,
    string FileName,
    string ContentType,
    string ObjectKey,
    string RecommendedAction);
