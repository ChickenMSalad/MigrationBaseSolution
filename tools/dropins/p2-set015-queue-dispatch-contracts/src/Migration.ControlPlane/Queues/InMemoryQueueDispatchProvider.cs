namespace Migration.ControlPlane.Queues;

public sealed class InMemoryQueueDispatchProvider : IQueueDispatchProvider
{
    public QueueDispatchProviderDescriptor Descriptor { get; }

    public InMemoryQueueDispatchProvider(string logicalQueueName)
    {
        Descriptor = new QueueDispatchProviderDescriptor(
            ProviderKind: "inMemory",
            LogicalQueueName: string.IsNullOrWhiteSpace(logicalQueueName) ? "migration-runs" : logicalQueueName,
            IsConfigured: true,
            SupportsNativeVisibilityTimeout: false,
            SupportsNativePoisonHandling: false,
            SupportsNativeMessageProperties: true,
            Warnings:
            [
                "In-memory queue dispatch is for local diagnostics only and does not persist messages."
            ]);
    }

    public Task<QueueDispatchResult> DispatchAsync(
        QueueMessageEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return Task.FromResult(new QueueDispatchResult(
            Accepted: true,
            ProviderKind: Descriptor.ProviderKind,
            LogicalQueueName: Descriptor.LogicalQueueName,
            MessageId: envelope.MessageId,
            IdempotencyKey: envelope.IdempotencyKey,
            DispatchedUtc: DateTimeOffset.UtcNow,
            ProviderMessageId: $"inmem-{envelope.MessageId}",
            Properties: envelope.Properties));
    }
}
