using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchCoordinator : IAzureWorkerDispatchCoordinator
{
    private readonly IAzureWorkerDispatchQueueReader queueReader;
    private readonly IAzureWorkerDispatchClaimStore claimStore;

    public AzureWorkerDispatchCoordinator(
        IAzureWorkerDispatchQueueReader queueReader,
        IAzureWorkerDispatchClaimStore claimStore)
    {
        this.queueReader = queueReader ?? throw new ArgumentNullException(nameof(queueReader));
        this.claimStore = claimStore ?? throw new ArgumentNullException(nameof(claimStore));
    }

    public async Task<AzureWorkerDispatchClaimBatchResult> ReadAndClaimAsync(
        AzureWorkerDispatchClaimBatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var readResult = await queueReader.ReadAsync(
            new AzureWorkerDispatchReadRequest
            {
                WorkerId = request.WorkerId,
                MaxMessages = Math.Max(1, request.MaxMessages),
                RequestedAtUtc = request.RequestedAtUtc,
                IncludeDelayedMessages = request.IncludeDelayedMessages
            },
            cancellationToken).ConfigureAwait(false);

        if (!readResult.HasMessages)
        {
            return AzureWorkerDispatchClaimBatchResult.Empty;
        }

        var claimedItems = new List<AzureWorkerDispatchClaimedWorkItem>();
        var rejectionReasons = new List<string>();

        foreach (var envelope in readResult.Envelopes)
        {
            var claimResult = await claimStore.TryClaimAsync(
                new AzureWorkerDispatchClaimRequest
                {
                    Envelope = envelope,
                    WorkerId = request.WorkerId,
                    LeaseDuration = request.ClaimLeaseDuration,
                    RequestedAtUtc = request.RequestedAtUtc
                },
                cancellationToken).ConfigureAwait(false);

            if (claimResult.Claimed && claimResult.Claim is not null)
            {
                claimedItems.Add(
                    new AzureWorkerDispatchClaimedWorkItem
                    {
                        Envelope = envelope,
                        Claim = claimResult.Claim
                    });
            }
            else if (!string.IsNullOrWhiteSpace(claimResult.Reason))
            {
                rejectionReasons.Add(claimResult.Reason);
            }
        }

        return new AzureWorkerDispatchClaimBatchResult
        {
            ClaimedItems = claimedItems,
            RejectionReasons = rejectionReasons
        };
    }
}
