using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalExecutionContextFactory : IOperationalExecutionContextFactory
{
    private readonly IOperationalStore _operationalStore;

    public OperationalExecutionContextFactory(
        IOperationalStore operationalStore)
    {
        _operationalStore = operationalStore;
    }

    public async Task<OperationalExecutionContext?> CreateAsync(
        Guid workItemId,
        CancellationToken cancellationToken = default)
    {
        var workItem = await _operationalStore.WorkItems.GetAsync(
            workItemId,
            cancellationToken);

        if (workItem is null)
        {
            return null;
        }

        var manifestRecord = await _operationalStore.ManifestRecords.GetAsync(
            workItem.ManifestRecordId,
            cancellationToken);

        if (manifestRecord is null)
        {
            return null;
        }

        return new OperationalExecutionContext(
            workItem.RunId,
            workItem.ManifestRecordId,
            workItem.WorkItemId,
            manifestRecord.SourceId,
            manifestRecord.SourcePath,
            manifestRecord.SourceName);
    }
}
