using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Operational.WorkItems;
using Migration.Workers.QueueExecutor.Options;

namespace Migration.Workers.QueueExecutor.Services;

public sealed class LoggingSqlOperationalWorkItemExecutor : ISqlOperationalWorkItemExecutor
{
    private readonly IOptions<SqlOperationalQueueExecutorOptions> _options;
    private readonly ILogger<LoggingSqlOperationalWorkItemExecutor> _logger;

    public LoggingSqlOperationalWorkItemExecutor(
        IOptions<SqlOperationalQueueExecutorOptions> options,
        ILogger<LoggingSqlOperationalWorkItemExecutor> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<SqlOperationalWorkItemExecutionResult> ExecuteAsync(
        OperationalWorkItemRecord workItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        _logger.LogInformation(
            "SQL operational work item claimed. WorkItemId={WorkItemId}; RunId={RunId}; ManifestRowId={ManifestRowId}; Type={WorkItemType}; Attempt={AttemptCount}/{MaxAttempts}; PayloadLength={PayloadLength}",
            workItem.WorkItemId,
            workItem.RunId,
            workItem.ManifestRowId,
            workItem.WorkItemType,
            workItem.AttemptCount,
            workItem.MaxAttempts,
            workItem.PayloadJson?.Length ?? 0);

        if (_options.Value.CompleteNoOpWorkItems)
        {
            return Task.FromResult(SqlOperationalWorkItemExecutionResult.Success(
                "{\"executor\":\"LoggingSqlOperationalWorkItemExecutor\",\"status\":\"CompletedNoOp\"}"));
        }

        var nextAttemptUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, _options.Value.RetryDelaySeconds));
        return Task.FromResult(SqlOperationalWorkItemExecutionResult.RetryableFailure(
            "SQL_OPERATIONAL_EXECUTOR_NOT_BOUND",
            "SQL operational work-item execution adapter is registered, but no real connector work-item executor has replaced the default logging executor.",
            nextAttemptUtc));
    }
}
