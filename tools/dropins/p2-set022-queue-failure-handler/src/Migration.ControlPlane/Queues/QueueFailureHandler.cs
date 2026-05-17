using System.Text;
using Migration.ControlPlane.Storage;

namespace Migration.ControlPlane.Queues;

public sealed class QueueFailureHandler : IQueueFailureHandler
{
    private readonly IArtifactStorageService _artifactStorage;
    private readonly IArtifactManifestIndexService _manifestIndex;

    public QueueFailureHandler(
        IArtifactStorageService artifactStorage,
        IArtifactManifestIndexService manifestIndex)
    {
        _artifactStorage = artifactStorage;
        _manifestIndex = manifestIndex;
    }

    public async Task<QueueFailureHandlingResult> HandleFailureAsync(
        QueueFailureArtifactRequest request,
        QueuePoisonHandlingPlan poisonPlan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(poisonPlan);

        var warnings = new List<string>();

        if (!poisonPlan.PersistFailureArtifact)
        {
            return new QueueFailureHandlingResult(
                FailureArtifactWritten: false,
                Strategy: poisonPlan.PoisonStrategy,
                ArtifactObjectKey: null,
                RecommendedNextAction: "Failure artifact persistence is disabled.",
                Warnings: warnings);
        }

        var descriptor = QueueFailureArtifactPlanner.BuildDescriptor(request, poisonPlan);
        var storageRequest = QueueFailureArtifactPlanner.ToArtifactStorageRequest(descriptor);
        var payload = QueueFailureArtifactPlanner.ToJsonPayload(request);

        await using var content = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var artifact = await _artifactStorage.WriteAsync(
            storageRequest,
            content,
            cancellationToken).ConfigureAwait(false);

        await _manifestIndex.AddAsync(artifact, cancellationToken).ConfigureAwait(false);

        if (request.Attempt < poisonPlan.MaxAttempts)
        {
            warnings.Add("Failure was recorded before max attempts were exhausted.");
        }

        return new QueueFailureHandlingResult(
            FailureArtifactWritten: true,
            Strategy: poisonPlan.PoisonStrategy,
            ArtifactObjectKey: artifact.ObjectKey,
            RecommendedNextAction: poisonPlan.PoisonStrategy,
            Warnings: warnings);
    }
}
