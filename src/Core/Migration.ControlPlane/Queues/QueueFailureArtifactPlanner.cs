using System.Text.Json;
using Migration.ControlPlane.Storage;

namespace Migration.ControlPlane.Queues;

public static class QueueFailureArtifactPlanner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static QueueFailureArtifactDescriptor BuildDescriptor(
        QueueFailureArtifactRequest request,
        QueuePoisonHandlingPlan poisonPlan)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(poisonPlan);

        var artifactKind = string.IsNullOrWhiteSpace(poisonPlan.FailureArtifactKind)
            ? "queue-failures"
            : Normalize(poisonPlan.FailureArtifactKind);

        var artifactId = $"{Normalize(request.ProjectId)}-{Normalize(request.RunId)}";
        var fileName = $"{Normalize(request.MessageType)}-{Normalize(request.IdempotencyKey)}-attempt-{request.Attempt}.json";

        return new QueueFailureArtifactDescriptor(
            WorkspaceId: Normalize(request.WorkspaceId),
            ArtifactKind: artifactKind,
            ArtifactId: artifactId,
            FileName: fileName,
            ContentType: "application/json",
            ObjectKey: $"{artifactKind}/{artifactId}/{fileName}",
            RecommendedAction: poisonPlan.PersistFailureArtifact
                ? "Persist this failure payload through artifact storage."
                : "Failure artifact persistence is disabled.");
    }

    public static ArtifactStorageRequest ToArtifactStorageRequest(
        QueueFailureArtifactDescriptor descriptor) =>
        new(
            WorkspaceId: descriptor.WorkspaceId,
            ArtifactKind: descriptor.ArtifactKind,
            ArtifactId: descriptor.ArtifactId,
            FileName: descriptor.FileName,
            ContentType: descriptor.ContentType);

    public static string ToJsonPayload(QueueFailureArtifactRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return JsonSerializer.Serialize(request, JsonOptions);
    }

    private static string Normalize(string value)
    {
        var sanitized = new string((value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' ? ch : '-')
            .ToArray());

        while (sanitized.Contains("--", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(sanitized)
            ? "unknown"
            : sanitized.Trim('-');
    }
}
