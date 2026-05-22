namespace Migration.ControlPlane.Queues;

public sealed class NullQueueDispatchProvider : IQueueDispatchProvider
{
    public QueueDispatchProviderDescriptor Descriptor { get; }

    public NullQueueDispatchProvider(string providerKind, string logicalQueueName, IReadOnlyList<string> warnings)
    {
        Descriptor = new QueueDispatchProviderDescriptor(
            ProviderKind: string.IsNullOrWhiteSpace(providerKind) ? "unknown" : providerKind,
            LogicalQueueName: string.IsNullOrWhiteSpace(logicalQueueName) ? "migration-runs" : logicalQueueName,
            IsConfigured: false,
            SupportsNativeVisibilityTimeout: false,
            SupportsNativePoisonHandling: false,
            SupportsNativeMessageProperties: false,
            Warnings: warnings);
    }

    public Task<QueueDispatchResult> DispatchAsync(
        QueueMessageEnvelope envelope,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            $"Queue dispatch provider '{Descriptor.ProviderKind}' is not configured.");
}
