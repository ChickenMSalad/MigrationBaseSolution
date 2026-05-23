using System;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Runtime.Workers.Leasing;

public sealed class NoOpAzureWorkerLeaseCoordinator : IAzureWorkerLeaseCoordinator
{
    public Task<AzureWorkerLeaseAcquisitionResult> TryAcquireAsync(
        AzureWorkerLeaseAcquisitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var acquiredAt = request.RequestedAtUtc;
        var expiresAt = acquiredAt.Add(request.LeaseDuration <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : request.LeaseDuration);
        var leaseId = $"noop:{request.WorkerId}:{request.WorkItemId}:{acquiredAt:yyyyMMddHHmmssfff}";

        return Task.FromResult(AzureWorkerLeaseAcquisitionResult.Granted(
            leaseId,
            request.WorkerId,
            request.WorkItemId,
            acquiredAt,
            expiresAt));
    }

    public Task<AzureWorkerLeaseRenewalResult> TryRenewAsync(
        AzureWorkerLeaseRenewalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var renewedAt = request.RequestedAtUtc;
        var expiresAt = renewedAt.Add(request.Extension <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : request.Extension);

        return Task.FromResult(AzureWorkerLeaseRenewalResult.Granted(request.LeaseId, renewedAt, expiresAt));
    }

    public Task ReleaseAsync(string leaseId, string workerId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
