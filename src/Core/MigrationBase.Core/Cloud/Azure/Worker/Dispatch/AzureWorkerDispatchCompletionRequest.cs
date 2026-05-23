using System;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchCompletionRequest
{
    public required AzureWorkerDispatchEnvelope Envelope { get; init; }

    public required AzureWorkerDispatchClaim Claim { get; init; }

    public required AzureWorkerDispatchCompletionStatus Status { get; init; }

    public string? Reason { get; init; }

    public string? ErrorCode { get; init; }

    public DateTimeOffset CompletedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RetryNotBeforeUtc { get; init; }
}
