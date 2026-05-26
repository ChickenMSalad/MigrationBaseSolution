using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalWorkItemDispatchService
{
    Task<OperationalQueueMessage?> DispatchAsync(
        Guid runId,
        long manifestRecordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalQueueMessage>> DispatchBatchAsync(
        Guid runId,
        IReadOnlyCollection<long> manifestRecordIds,
        CancellationToken cancellationToken = default);
}
