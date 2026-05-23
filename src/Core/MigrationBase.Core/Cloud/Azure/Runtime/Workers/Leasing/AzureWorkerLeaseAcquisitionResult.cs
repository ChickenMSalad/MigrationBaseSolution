using System;

namespace MigrationBase.Core.Cloud.Azure.Runtime.Workers.Leasing;

public sealed class AzureWorkerLeaseAcquisitionResult
{
    public required bool Acquired { get; init; }
    public string? LeaseId { get; init; }
    public string? WorkerId { get; init; }
    public string? WorkItemId { get; init; }
    public DateTimeOffset? AcquiredAtUtc { get; init; }
    public DateTimeOffset? ExpiresAtUtc { get; init; }
    public string? NotAcquiredReason { get; init; }

    public static AzureWorkerLeaseAcquisitionResult Granted(
        string leaseId,
        string workerId,
        string workItemId,
        DateTimeOffset acquiredAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        return new AzureWorkerLeaseAcquisitionResult
        {
            Acquired = true,
            LeaseId = leaseId,
            WorkerId = workerId,
            WorkItemId = workItemId,
            AcquiredAtUtc = acquiredAtUtc,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public static AzureWorkerLeaseAcquisitionResult Rejected(string reason)
    {
        return new AzureWorkerLeaseAcquisitionResult
        {
            Acquired = false,
            NotAcquiredReason = string.IsNullOrWhiteSpace(reason) ? "Lease was not acquired." : reason
        };
    }
}
