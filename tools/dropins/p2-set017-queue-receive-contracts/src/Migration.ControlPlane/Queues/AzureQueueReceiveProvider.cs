using Azure.Identity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace Migration.ControlPlane.Queues;

public sealed class AzureQueueReceiveProvider : IQueueReceiveProvider
{
    private readonly AzureQueueDispatchOptions _options;

    public AzureQueueReceiveProvider(AzureQueueDispatchOptions options)
    {
        _options = options;
        Descriptor = new QueueReceiveProviderDescriptor(
            ProviderKind: "azureStorageQueue",
            LogicalQueueName: options.QueueName,
            IsConfigured: options.IsConfigured,
            SupportsVisibilityTimeout: true,
            SupportsNativeDequeueCount: true,
            SupportsAbandon: true,
            Warnings:
            [
                "Azure Storage Queue abandon is implemented by leaving the message until visibility timeout expires."
            ]);
    }

    public QueueReceiveProviderDescriptor Descriptor { get; }

    public async Task<IReadOnlyList<QueueReceivedMessage>> ReceiveAsync(
        int maxMessages = 1,
        TimeSpan? visibilityTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var client = CreateQueueClient();
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var response = await client.ReceiveMessagesAsync(
            maxMessages: Math.Clamp(maxMessages, 1, 32),
            visibilityTimeout: visibilityTimeout ?? TimeSpan.FromMinutes(5),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return response.Value
            .Select(ToReceivedMessage)
            .ToArray();
    }

    public async Task CompleteAsync(
        QueueReceivedMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.PopReceipt))
        {
            throw new InvalidOperationException("Cannot complete Azure Queue message without pop receipt.");
        }

        var client = CreateQueueClient();
        await client.DeleteMessageAsync(
            message.ProviderMessageId,
            message.PopReceipt,
            cancellationToken).ConfigureAwait(false);
    }

    public Task AbandonAsync(
        QueueReceivedMessage message,
        CancellationToken cancellationToken = default)
    {
        // Azure Storage Queue abandon means letting the visibility timeout expire.
        return Task.CompletedTask;
    }

    private QueueReceivedMessage ToReceivedMessage(QueueMessage message)
    {
        var envelope = QueueMessageSerialization.FromBase64Json(message.MessageText);

        return new QueueReceivedMessage(
            ProviderKind: Descriptor.ProviderKind,
            LogicalQueueName: Descriptor.LogicalQueueName,
            ProviderMessageId: message.MessageId,
            PopReceipt: message.PopReceipt,
            DequeueCount: message.DequeueCount,
            Envelope: envelope,
            ReceivedUtc: DateTimeOffset.UtcNow);
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

        throw new InvalidOperationException("Azure Queue receive is selected but no ServiceUri, AccountName, or ConnectionString is configured.");
    }
}
