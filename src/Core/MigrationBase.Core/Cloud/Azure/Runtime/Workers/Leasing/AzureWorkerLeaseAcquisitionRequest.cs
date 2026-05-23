using System;

namespace MigrationBase.Core.Cloud.Azure.Runtime.Workers.Leasing;

public sealed class AzureWorkerLeaseAcquisitionRequest
{
    public required string WorkerId { get; init; }
    public required string WorkItemId { get; init; }
    public required string QueueName { get; init; }
    public string? RunId { get; init; }
    public string? TenantId { get; init; }
    public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromMinutes(5);
    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
