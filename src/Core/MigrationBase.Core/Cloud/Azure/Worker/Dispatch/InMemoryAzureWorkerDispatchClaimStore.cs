namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class InMemoryAzureWorkerDispatchClaimStore : IAzureWorkerDispatchClaimStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, AzureWorkerDispatchClaim> claims =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<AzureWorkerDispatchClaimResult> TryClaimAsync(
        AzureWorkerDispatchClaimRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Envelope);

        cancellationToken.ThrowIfCancellationRequested();

        lock (gate)
        {
            var now = request.RequestedAtUtc;

            if (claims.TryGetValue(request.Envelope.WorkItemId, out var existing) &&
                !existing.IsExpired(now))
            {
                return Task.FromResult(
                    AzureWorkerDispatchClaimResult.Rejected(
                        "Work item is already claimed by an active worker lease."));
            }

            var claim = new AzureWorkerDispatchClaim
            {
                ClaimId = Guid.NewGuid().ToString("n"),
                DispatchId = request.Envelope.DispatchId,
                WorkItemId = request.Envelope.WorkItemId,
                WorkerId = request.WorkerId,
                ClaimedAtUtc = now,
                ExpiresAtUtc = now.Add(request.LeaseDuration)
            };

            claims[request.Envelope.WorkItemId] = claim;

            return Task.FromResult(AzureWorkerDispatchClaimResult.Accepted(claim));
        }
    }

    public Task<bool> ReleaseAsync(
        AzureWorkerDispatchClaim claim,
        string reason,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claim);
        cancellationToken.ThrowIfCancellationRequested();

        lock (gate)
        {
            if (!claims.TryGetValue(claim.WorkItemId, out var existing))
            {
                return Task.FromResult(false);
            }

            if (!StringComparer.OrdinalIgnoreCase.Equals(existing.ClaimId, claim.ClaimId))
            {
                return Task.FromResult(false);
            }

            claims.Remove(claim.WorkItemId);
            return Task.FromResult(true);
        }
    }
}
