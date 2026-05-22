using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalQueuePublisher
{
    Task PublishAsync(
        OperationalQueueMessage message,
        CancellationToken cancellationToken = default);
}
