using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalWorkItemDispatchService : IOperationalWorkItemDispatchService
{
    private readonly IOperationalWorkItemLifecycleService _workItemLifecycleService;
    private readonly IOperationalWorkItemQueuePublisher _workItemQueuePublisher;

    public OperationalWorkItemDispatchService(
        IOperationalWorkItemLifecycleService workItemLifecycleService,
        IOperationalWorkItemQueuePublisher workItemQueuePublisher)
    {
        _workItemLifecycleService = workItemLifecycleService;
        _workItemQueuePublisher = workItemQueuePublisher;
    }

    public async Task<OperationalQueueMessage?> DispatchAsync(
        Guid runId,
        long manifestRecordId,
        CancellationToken cancellationToken = default)
    {
        var workItem = await _workItemLifecycleService.CreateWorkItemAsync(
            runId,
            manifestRecordId,
            cancellationToken);

        return await _workItemQueuePublisher.PublishAsync(
            workItem.WorkItemId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<OperationalQueueMessage>> DispatchBatchAsync(
        Guid runId,
        IReadOnlyCollection<long> manifestRecordIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifestRecordIds);

        if (manifestRecordIds.Count == 0)
        {
            return Array.Empty<OperationalQueueMessage>();
        }

        var workItems = await _workItemLifecycleService.CreateWorkItemBatchAsync(
            runId,
            manifestRecordIds,
            cancellationToken);

        var messages = new List<OperationalQueueMessage>();

        foreach (var workItem in workItems)
        {
            var message = await _workItemQueuePublisher.PublishAsync(
                workItem.WorkItemId,
                cancellationToken);

            if (message is not null)
            {
                messages.Add(message);
            }
        }

        return messages;
    }
}
