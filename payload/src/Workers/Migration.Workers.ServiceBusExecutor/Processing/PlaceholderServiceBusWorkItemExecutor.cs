using Migration.Application.Operational.WorkItems;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Workers.ServiceBusExecutor.Options;

namespace Migration.Workers.ServiceBusExecutor.Processing;

internal sealed class PlaceholderServiceBusWorkItemExecutor : IServiceBusWorkItemExecutor
{
    private readonly ILogger<PlaceholderServiceBusWorkItemExecutor> _logger;
    private readonly IOptions<SqlServiceBusExecutorOptions> _options;

    public PlaceholderServiceBusWorkItemExecutor(
        ILogger<PlaceholderServiceBusWorkItemExecutor> logger,
        IOptions<SqlServiceBusExecutorOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<ServiceBusWorkItemExecutionResult> ExecuteAsync(
        OperationalWorkItemRecord workItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        if (!_options.Value.CompleteWithoutExecutingMigration)
        {
            _logger.LogInformation(
                "Work item {WorkItemId} was received by the Service Bus executor, but migration execution is intentionally disabled.",
                workItem.WorkItemId);

            return Task.FromResult(new ServiceBusWorkItemExecutionResult(
                Succeeded: false,
                ResultJson: null,
                ErrorCode: "EXECUTION_DISABLED",
                ErrorMessage: "Service Bus executor is wired, but actual migration execution is not enabled for this worker yet.",
                IsRetryable: true));
        }

        _logger.LogInformation(
            "Completing work item {WorkItemId} using placeholder execution mode.",
            workItem.WorkItemId);

        return Task.FromResult(new ServiceBusWorkItemExecutionResult(
            Succeeded: true,
            ResultJson: "{\"status\":\"completed-by-placeholder-executor\"}",
            ErrorCode: null,
            ErrorMessage: null,
            IsRetryable: false));
    }
}
