using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class InMemoryAzureWorkerDispatchCompletionSink : IAzureWorkerDispatchCompletionSink
{
    private readonly IAzureWorkerDispatchClaimStore claimStore;
    private readonly List<AzureWorkerDispatchCompletionRequest> completions = new();

    public InMemoryAzureWorkerDispatchCompletionSink(
        IAzureWorkerDispatchClaimStore claimStore)
    {
        this.claimStore = claimStore ?? throw new ArgumentNullException(nameof(claimStore));
    }

    public IReadOnlyList<AzureWorkerDispatchCompletionRequest> Completions => completions;

    public async Task<AzureWorkerDispatchCompletionResult> CompleteAsync(
        AzureWorkerDispatchCompletionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Envelope);
        ArgumentNullException.ThrowIfNull(request.Claim);

        cancellationToken.ThrowIfCancellationRequested();

        if (!StringComparer.OrdinalIgnoreCase.Equals(
                request.Envelope.WorkItemId,
                request.Claim.WorkItemId))
        {
            return AzureWorkerDispatchCompletionResult.Rejected(
                "Completion envelope and claim refer to different work items.");
        }

        completions.Add(request);

        var releaseReason = string.IsNullOrWhiteSpace(request.Reason)
            ? request.Status.ToString()
            : request.Reason;

        var claimReleased = await claimStore.ReleaseAsync(
            request.Claim,
            releaseReason,
            cancellationToken).ConfigureAwait(false);

        return AzureWorkerDispatchCompletionResult.Completed(claimReleased);
    }
}
