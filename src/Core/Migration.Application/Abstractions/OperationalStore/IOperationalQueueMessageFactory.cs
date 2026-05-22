using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalQueueMessageFactory
{
    Task<OperationalQueueMessage?> CreateAsync(
        Guid workItemId,
        CancellationToken cancellationToken = default);
}
