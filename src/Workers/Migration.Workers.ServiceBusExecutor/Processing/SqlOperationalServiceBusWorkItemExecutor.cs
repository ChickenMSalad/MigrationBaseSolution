using Migration.Application.Operational.WorkItems;
using Migration.Workers.QueueExecutor.Services;

namespace Migration.Workers.ServiceBusExecutor.Processing;

internal sealed class SqlOperationalServiceBusWorkItemExecutor : IServiceBusWorkItemExecutor
{
    private readonly ISqlOperationalWorkItemExecutor _executor;

    public SqlOperationalServiceBusWorkItemExecutor(ISqlOperationalWorkItemExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public async Task<ServiceBusWorkItemExecutionResult> ExecuteAsync(
        OperationalWorkItemRecord workItem,
        CancellationToken cancellationToken = default)
    {
        var result = await _executor.ExecuteAsync(workItem, cancellationToken).ConfigureAwait(false);

        return new ServiceBusWorkItemExecutionResult(
            result.Succeeded,
            result.ResultJson,
            result.ErrorCode,
            result.ErrorMessage,
            result.IsRetryable);
    }
}