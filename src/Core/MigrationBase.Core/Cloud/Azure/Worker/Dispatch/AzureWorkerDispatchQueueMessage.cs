using System;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchQueueMessage
{
    public required string MessageId { get; init; }

    public required AzureWorkerDispatchEnvelope Envelope { get; init; }

    public DateTimeOffset ReceivedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public int DeliveryCount { get; init; }

    public string? ReceiptHandle { get; init; }
}
