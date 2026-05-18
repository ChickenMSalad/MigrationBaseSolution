using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalWorkItemExecutionSynchronizer : IOperationalWorkItemExecutionSynchronizer
{
    private readonly IOperationalExecutionContextFactory _executionContextFactory;
    private readonly IOperationalManifestLifecycleService _manifestLifecycleService;
    private readonly IOperationalWorkItemLifecycleService _workItemLifecycleService;

    public OperationalWorkItemExecutionSynchronizer(
        IOperationalExecutionContextFactory executionContextFactory,
        IOperationalManifestLifecycleService manifestLifecycleService,
        IOperationalWorkItemLifecycleService workItemLifecycleService)
    {
        _executionContextFactory = executionContextFactory;
        _manifestLifecycleService = manifestLifecycleService;
        _workItemLifecycleService = workItemLifecycleService;
    }

    public async Task<OperationalExecutionContext?> BeginAsync(
        Guid workItemId,
        string lockedBy,
        CancellationToken cancellationToken = default)
    {
        var context = await _executionContextFactory.CreateAsync(
            workItemId,
            cancellationToken);

        if (context is null)
        {
            return null;
        }

        await _workItemLifecycleService.MarkWorkItemLockedAsync(
            context.WorkItemId,
            lockedBy,
            cancellationToken);

        await _manifestLifecycleService.MarkManifestProcessingAsync(
            context.ManifestRecordId,
            cancellationToken);

        return context;
    }

    public async Task CompleteAsync(
        OperationalExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _workItemLifecycleService.MarkWorkItemCompletedAsync(
            context.WorkItemId,
            cancellationToken);

        await _manifestLifecycleService.MarkManifestCompletedAsync(
            context.ManifestRecordId,
            cancellationToken);
    }

    public async Task FailAsync(
        OperationalExecutionContext context,
        string failureReason,
        bool isRetriable,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _workItemLifecycleService.MarkWorkItemFailedAsync(
            context.RunId,
            context.WorkItemId,
            context.ManifestRecordId,
            failureReason,
            isRetriable,
            cancellationToken);

        await _manifestLifecycleService.MarkManifestFailedAsync(
            context.RunId,
            context.ManifestRecordId,
            failureReason,
            isRetriable,
            cancellationToken);
    }
}
