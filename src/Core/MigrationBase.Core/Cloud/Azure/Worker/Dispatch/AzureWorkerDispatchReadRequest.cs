using System;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchReadRequest
{
    public required string WorkerId { get; init; }

    public int MaxMessages { get; init; } = 1;

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool IncludeDelayedMessages { get; init; }
}
