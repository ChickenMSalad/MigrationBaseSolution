using System;

namespace MigrationBase.Core.Cloud.Azure.Runtime.Workers.Leasing;

public sealed class AzureWorkerLeaseRenewalResult
{
    public required bool Renewed { get; init; }
    public string? LeaseId { get; init; }
    public DateTimeOffset? RenewedAtUtc { get; init; }
    public DateTimeOffset? ExpiresAtUtc { get; init; }
    public string? NotRenewedReason { get; init; }

    public static AzureWorkerLeaseRenewalResult Granted(string leaseId, DateTimeOffset renewedAtUtc, DateTimeOffset expiresAtUtc)
    {
        return new AzureWorkerLeaseRenewalResult
        {
            Renewed = true,
            LeaseId = leaseId,
            RenewedAtUtc = renewedAtUtc,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public static AzureWorkerLeaseRenewalResult Rejected(string leaseId, string reason)
    {
        return new AzureWorkerLeaseRenewalResult
        {
            Renewed = false,
            LeaseId = leaseId,
            NotRenewedReason = string.IsNullOrWhiteSpace(reason) ? "Lease was not renewed." : reason
        };
    }
}
