using System;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchClaimBatchRequest
{
    public required string WorkerId { get; init; }

    public int MaxMessages { get; init; } = 1;

    public TimeSpan ClaimLeaseDuration { get; init; } = TimeSpan.FromMinutes(5);

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool IncludeDelayedMessages { get; init; }
}
