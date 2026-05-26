using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalQueueMessageFactory
{
    Task<OperationalQueueMessage?> CreateAsync(
        long workItemId,
        CancellationToken cancellationToken = default);
}
