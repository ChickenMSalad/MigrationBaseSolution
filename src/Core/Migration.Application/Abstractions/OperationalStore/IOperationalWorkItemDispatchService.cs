using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalWorkItemDispatchService
{
    Task<OperationalQueueMessage?> DispatchAsync(
        Guid runId,
        Guid manifestRecordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalQueueMessage>> DispatchBatchAsync(
        Guid runId,
        IReadOnlyCollection<Guid> manifestRecordIds,
        CancellationToken cancellationToken = default);
}
