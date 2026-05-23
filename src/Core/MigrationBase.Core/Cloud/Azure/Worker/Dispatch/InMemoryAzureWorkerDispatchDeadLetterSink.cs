using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class InMemoryAzureWorkerDispatchDeadLetterSink : IAzureWorkerDispatchDeadLetterSink
{
    private readonly IAzureWorkerDispatchClaimStore claimStore;
    private readonly List<AzureWorkerDispatchDeadLetterRequest> deadLetters = new();

    public InMemoryAzureWorkerDispatchDeadLetterSink(
        IAzureWorkerDispatchClaimStore claimStore)
    {
        this.claimStore = claimStore ?? throw new ArgumentNullException(nameof(claimStore));
    }

    public IReadOnlyList<AzureWorkerDispatchDeadLetterRequest> DeadLetters => deadLetters;

    public async Task<AzureWorkerDispatchDeadLetterResult> DeadLetterAsync(
        AzureWorkerDispatchDeadLetterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Envelope);

        cancellationToken.ThrowIfCancellationRequested();

        deadLetters.Add(request);

        if (request.Claim is null)
        {
            return AzureWorkerDispatchDeadLetterResult.AcceptedResult(false);
        }

        if (!StringComparer.OrdinalIgnoreCase.Equals(
                request.Envelope.WorkItemId,
                request.Claim.WorkItemId))
        {
            return AzureWorkerDispatchDeadLetterResult.Rejected(
                "Dead-letter envelope and claim refer to different work items.");
        }

        var releaseReason = string.IsNullOrWhiteSpace(request.Details)
            ? request.Reason.ToString()
            : request.Details;

        var claimReleased = await claimStore.ReleaseAsync(
            request.Claim,
            releaseReason,
            cancellationToken).ConfigureAwait(false);

        return AzureWorkerDispatchDeadLetterResult.AcceptedResult(claimReleased);
    }
}
