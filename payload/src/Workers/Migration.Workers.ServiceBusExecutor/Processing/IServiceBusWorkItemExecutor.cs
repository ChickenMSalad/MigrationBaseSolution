using Migration.Application.Operational.WorkItems;

namespace Migration.Workers.ServiceBusExecutor.Processing;

internal interface IServiceBusWorkItemExecutor
{
    Task<ServiceBusWorkItemExecutionResult> ExecuteAsync(
        OperationalWorkItemRecord workItem,
        CancellationToken cancellationToken = default);
}

internal sealed record ServiceBusWorkItemExecutionResult(
    bool Succeeded,
    string? ResultJson,
    string? ErrorCode,
    string? ErrorMessage,
    bool IsRetryable);
