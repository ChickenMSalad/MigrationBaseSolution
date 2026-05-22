namespace Migration.ControlPlane.Queues;

public sealed class InMemoryQueueReceiveProvider : IQueueReceiveProvider
{
    public InMemoryQueueReceiveProvider(string logicalQueueName)
    {
        Descriptor = new QueueReceiveProviderDescriptor(
            ProviderKind: "inMemory",
            LogicalQueueName: string.IsNullOrWhiteSpace(logicalQueueName) ? "migration-runs" : logicalQueueName,
            IsConfigured: true,
            SupportsVisibilityTimeout: false,
            SupportsNativeDequeueCount: false,
            SupportsAbandon: false,
            Warnings:
            [
                "In-memory receive provider is diagnostics-only and does not persist or receive queued messages."
            ]);
    }

    public QueueReceiveProviderDescriptor Descriptor { get; }

    public Task<IReadOnlyList<QueueReceivedMessage>> ReceiveAsync(
        int maxMessages = 1,
        TimeSpan? visibilityTimeout = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QueueReceivedMessage>>(Array.Empty<QueueReceivedMessage>());

    public Task CompleteAsync(
        QueueReceivedMessage message,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task AbandonAsync(
        QueueReceivedMessage message,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
