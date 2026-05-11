using Migration.Application.Abstractions;
using Migration.Domain.Models;
using Migration.Orchestration.Abstractions;

namespace Migration.Orchestration.State;

public sealed class LegacyJobStateStoreBridge : IJobStateStore
{
    private readonly IMigrationExecutionStateStore _stateStore;

    public LegacyJobStateStoreBridge(IMigrationExecutionStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public Task SaveCheckpointAsync(CheckpointRecord checkpoint, CancellationToken cancellationToken = default)
    {
        return _stateStore.SaveWorkItemAsync(new MigrationWorkItemState
        {
            RunId = checkpoint.JobName,
            JobName = checkpoint.JobName,
            WorkItemId = checkpoint.WorkItemId,
            Status = checkpoint.Status,
            Message = checkpoint.Detail,
            UpdatedUtc = checkpoint.UpdatedUtc
        }, cancellationToken);
    }
}
