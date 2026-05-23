using System;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchDeadLetterRequest
{
    public required AzureWorkerDispatchEnvelope Envelope { get; init; }

    public AzureWorkerDispatchClaim? Claim { get; init; }

    public required AzureWorkerDispatchDeadLetterReason Reason { get; init; }

    public string? Details { get; init; }

    public string? ErrorCode { get; init; }

    public DateTimeOffset DeadLetteredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
