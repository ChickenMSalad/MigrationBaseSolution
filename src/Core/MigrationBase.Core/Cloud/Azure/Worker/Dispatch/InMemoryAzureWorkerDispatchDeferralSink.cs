using System;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class InMemoryAzureWorkerDispatchDeferralSink : IAzureWorkerDispatchDeferralSink
{
    private readonly IAzureWorkerDispatchQueueWriter queueWriter;
    private readonly IAzureWorkerDispatchClaimStore claimStore;

    public InMemoryAzureWorkerDispatchDeferralSink(
        IAzureWorkerDispatchQueueWriter queueWriter,
        IAzureWorkerDispatchClaimStore claimStore)
    {
        this.queueWriter = queueWriter ?? throw new ArgumentNullException(nameof(queueWriter));
        this.claimStore = claimStore ?? throw new ArgumentNullException(nameof(claimStore));
    }

    public async Task<AzureWorkerDispatchCompletionResult> DeferAsync(
        AzureWorkerDispatchDeferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Envelope);

        cancellationToken.ThrowIfCancellationRequested();

        var deferredEnvelope = new AzureWorkerDispatchEnvelope
        {
            DispatchId = request.Envelope.DispatchId,
            WorkItemId = request.Envelope.WorkItemId,
            RunId = request.Envelope.RunId,
            ManifestId = request.Envelope.ManifestId,
            SourceSystem = request.Envelope.SourceSystem,
            TargetSystem = request.Envelope.TargetSystem,
            AttemptNumber = request.Envelope.AttemptNumber,
            EnqueuedAtUtc = DateTimeOffset.UtcNow,
            NotBeforeUtc = request.NotBeforeUtc,
            Attributes = request.Envelope.Attributes
        };

        await queueWriter.EnqueueAsync(
            deferredEnvelope,
            cancellationToken).ConfigureAwait(false);

        var claimReleased = false;

        if (request.Claim is not null)
        {
            claimReleased = await claimStore.ReleaseAsync(
                request.Claim,
                string.IsNullOrWhiteSpace(request.Reason) ? "Deferred" : request.Reason,
                cancellationToken).ConfigureAwait(false);
        }

        return AzureWorkerDispatchCompletionResult.Completed(claimReleased);
    }
}
