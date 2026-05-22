namespace Migration.ControlPlane.Queues;

public interface IQueueDispatchProvider
{
    QueueDispatchProviderDescriptor Descriptor { get; }

    Task<QueueDispatchResult> DispatchAsync(
        QueueMessageEnvelope envelope,
        CancellationToken cancellationToken = default);
}

public sealed record QueueDispatchProviderDescriptor(
    string ProviderKind,
    string LogicalQueueName,
    bool IsConfigured,
    bool SupportsNativeVisibilityTimeout,
    bool SupportsNativePoisonHandling,
    bool SupportsNativeMessageProperties,
    IReadOnlyList<string> Warnings);

public sealed record QueueDispatchResult(
    bool Accepted,
    string ProviderKind,
    string LogicalQueueName,
    string MessageId,
    string IdempotencyKey,
    DateTimeOffset DispatchedUtc,
    string? ProviderMessageId = null,
    IReadOnlyDictionary<string, string>? Properties = null);
