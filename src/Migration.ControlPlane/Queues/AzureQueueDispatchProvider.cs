using Azure.Identity;
using Azure.Storage.Queues;

namespace Migration.ControlPlane.Queues;

public sealed class AzureQueueDispatchProvider : IQueueDispatchProvider
{
    private readonly AzureQueueDispatchOptions _options;

    public AzureQueueDispatchProvider(AzureQueueDispatchOptions options)
    {
        _options = options;
        Descriptor = new QueueDispatchProviderDescriptor(
            ProviderKind: "azureStorageQueue",
            LogicalQueueName: options.QueueName,
            IsConfigured: options.IsConfigured,
            SupportsNativeVisibilityTimeout: true,
            SupportsNativePoisonHandling: false,
            SupportsNativeMessageProperties: false,
            Warnings:
            [
                "Azure Storage Queue does not support native sessions or dead lettering.",
                "Message properties are serialized into the message envelope payload."
            ]);
    }

    public QueueDispatchProviderDescriptor Descriptor { get; }

    public async Task<QueueDispatchResult> DispatchAsync(
        QueueMessageEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var client = CreateQueueClient();
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var payload = QueueMessageSerialization.ToBase64Json(envelope);
        var response = await client.SendMessageAsync(
            payload,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new QueueDispatchResult(
            Accepted: true,
            ProviderKind: Descriptor.ProviderKind,
            LogicalQueueName: Descriptor.LogicalQueueName,
            MessageId: envelope.MessageId,
            IdempotencyKey: envelope.IdempotencyKey,
            DispatchedUtc: DateTimeOffset.UtcNow,
            ProviderMessageId: response.Value.MessageId,
            Properties: envelope.Properties);
    }

    private QueueClient CreateQueueClient()
    {
        if (!string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return new QueueClient(_options.ConnectionString, _options.QueueName);
        }

        var serviceUri = ResolveServiceUri();
        var service = new QueueServiceClient(serviceUri, new DefaultAzureCredential());
        return service.GetQueueClient(_options.QueueName);
    }

    private Uri ResolveServiceUri()
    {
        if (!string.IsNullOrWhiteSpace(_options.ServiceUri))
        {
            return new Uri(_options.ServiceUri);
        }

        if (!string.IsNullOrWhiteSpace(_options.AccountName))
        {
            return new Uri($"https://{_options.AccountName}.queue.core.windows.net/");
        }

        throw new InvalidOperationException("Azure Queue dispatch is selected but no ServiceUri, AccountName, or ConnectionString is configured.");
    }
}
