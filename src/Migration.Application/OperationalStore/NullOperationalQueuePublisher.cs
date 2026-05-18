using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class NullOperationalQueuePublisher : IOperationalQueuePublisher
{
    public Task PublishAsync(
        OperationalQueueMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return Task.CompletedTask;
    }
}
