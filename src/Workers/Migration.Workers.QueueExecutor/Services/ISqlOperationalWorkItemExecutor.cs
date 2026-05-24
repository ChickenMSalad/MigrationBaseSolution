using Migration.Application.Operational.WorkItems;

namespace Migration.Workers.QueueExecutor.Services;

public interface ISqlOperationalWorkItemExecutor
{
    Task<SqlOperationalWorkItemExecutionResult> ExecuteAsync(
        OperationalWorkItemRecord workItem,
        CancellationToken cancellationToken = default);
}
