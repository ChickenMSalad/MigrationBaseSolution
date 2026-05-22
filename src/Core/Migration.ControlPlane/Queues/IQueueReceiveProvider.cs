namespace Migration.ControlPlane.Queues;

public interface IQueueReceiveProvider
{
    QueueReceiveProviderDescriptor Descriptor { get; }

    Task<IReadOnlyList<QueueReceivedMessage>> ReceiveAsync(
        int maxMessages = 1,
        TimeSpan? visibilityTimeout = null,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        QueueReceivedMessage message,
        CancellationToken cancellationToken = default);

    Task AbandonAsync(
        QueueReceivedMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record QueueReceiveProviderDescriptor(
    string ProviderKind,
    string LogicalQueueName,
    bool IsConfigured,
    bool SupportsVisibilityTimeout,
    bool SupportsNativeDequeueCount,
    bool SupportsAbandon,
    IReadOnlyList<string> Warnings);

public sealed record QueueReceivedMessage(
    string ProviderKind,
    string LogicalQueueName,
    string ProviderMessageId,
    string? PopReceipt,
    int? DequeueCount,
    QueueMessageEnvelope Envelope,
    DateTimeOffset ReceivedUtc);
