using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalWorkItemQueuePublisher
{
    Task<OperationalQueueMessage?> PublishAsync(
        Guid workItemId,
        CancellationToken cancellationToken = default);
}
