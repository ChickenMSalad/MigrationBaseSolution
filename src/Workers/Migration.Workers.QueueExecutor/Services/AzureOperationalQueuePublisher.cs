using Azure.Storage.Queues;
using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.OperationalStore;
using Migration.Workers.QueueExecutor.Options;
using Microsoft.Extensions.Options;

namespace Migration.Workers.QueueExecutor.Services;

public sealed class AzureOperationalQueuePublisher : IOperationalQueuePublisher
{
    private readonly IOperationalQueueMessageSerializer _serializer;
    private readonly IOptions<OperationalQueuePublisherOptions> _options;

    public AzureOperationalQueuePublisher(
        IOperationalQueueMessageSerializer serializer,
        IOptions<OperationalQueuePublisherOptions> options)
    {
        _serializer = serializer;
        _options = options;
    }

    public async Task PublishAsync(
        OperationalQueueMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException(
                "Operational queue publisher connection string is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.QueueName))
        {
            throw new InvalidOperationException(
                "Operational queue publisher queue name is not configured.");
        }

        var queueClient = new QueueClient(
            options.ConnectionString,
            options.QueueName);

        await queueClient.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken);

        var payload = _serializer.Serialize(message);

        await queueClient.SendMessageAsync(
            payload,
            cancellationToken);
    }
}
