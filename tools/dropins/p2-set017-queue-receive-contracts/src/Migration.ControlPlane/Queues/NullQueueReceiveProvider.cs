namespace Migration.ControlPlane.Queues;

public sealed class NullQueueReceiveProvider : IQueueReceiveProvider
{
    public NullQueueReceiveProvider(
        string providerKind,
        string logicalQueueName,
        IReadOnlyList<string> warnings)
    {
        Descriptor = new QueueReceiveProviderDescriptor(
            ProviderKind: string.IsNullOrWhiteSpace(providerKind) ? "unknown" : providerKind,
            LogicalQueueName: string.IsNullOrWhiteSpace(logicalQueueName) ? "migration-runs" : logicalQueueName,
            IsConfigured: false,
            SupportsVisibilityTimeout: false,
            SupportsNativeDequeueCount: false,
            SupportsAbandon: false,
            Warnings: warnings);
    }

    public QueueReceiveProviderDescriptor Descriptor { get; }

    public Task<IReadOnlyList<QueueReceivedMessage>> ReceiveAsync(
        int maxMessages = 1,
        TimeSpan? visibilityTimeout = null,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            $"Queue receive provider '{Descriptor.ProviderKind}' is not configured.");

    public Task CompleteAsync(
        QueueReceivedMessage message,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            $"Queue receive provider '{Descriptor.ProviderKind}' is not configured.");

    public Task AbandonAsync(
        QueueReceivedMessage message,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            $"Queue receive provider '{Descriptor.ProviderKind}' is not configured.");
}
