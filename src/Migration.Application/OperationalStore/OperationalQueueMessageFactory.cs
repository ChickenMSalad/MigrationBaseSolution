using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalQueueMessageFactory : IOperationalQueueMessageFactory
{
    private readonly IOperationalExecutionContextFactory _executionContextFactory;

    public OperationalQueueMessageFactory(
        IOperationalExecutionContextFactory executionContextFactory)
    {
        _executionContextFactory = executionContextFactory;
    }

    public async Task<OperationalQueueMessage?> CreateAsync(
        Guid workItemId,
        CancellationToken cancellationToken = default)
    {
        var context = await _executionContextFactory.CreateAsync(
            workItemId,
            cancellationToken);

        if (context is null)
        {
            return null;
        }

        return new OperationalQueueMessage
        {
            RunId = context.RunId,
            ManifestRecordId = context.ManifestRecordId,
            WorkItemId = context.WorkItemId,
            SourceId = context.SourceId,
            SourcePath = context.SourcePath,
            SourceName = context.SourceName
        };
    }
}
