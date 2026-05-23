using System;

namespace MigrationBase.Core.Cloud.Azure.Runtime.Workers.Leasing;

public sealed class AzureWorkerLeaseRenewalRequest
{
    public required string LeaseId { get; init; }
    public required string WorkerId { get; init; }
    public required string WorkItemId { get; init; }
    public TimeSpan Extension { get; init; } = TimeSpan.FromMinutes(5);
    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
