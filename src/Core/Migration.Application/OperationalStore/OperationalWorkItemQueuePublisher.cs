using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalWorkItemQueuePublisher : IOperationalWorkItemQueuePublisher
{
    private readonly IOperationalQueueMessageFactory _messageFactory;
    private readonly IOperationalQueuePublisher _queuePublisher;

    public OperationalWorkItemQueuePublisher(
        IOperationalQueueMessageFactory messageFactory,
        IOperationalQueuePublisher queuePublisher)
    {
        _messageFactory = messageFactory;
        _queuePublisher = queuePublisher;
    }

    public async Task<OperationalQueueMessage?> PublishAsync(
        long workItemId,
        CancellationToken cancellationToken = default)
    {
        var message = await _messageFactory.CreateAsync(
            workItemId,
            cancellationToken);

        if (message is null)
        {
            return null;
        }

        await _queuePublisher.PublishAsync(
            message,
            cancellationToken);

        return message;
    }
}
