using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalWorkItemExecutionSynchronizer
{
    Task<OperationalExecutionContext?> BeginAsync(
        long workItemId,
        string lockedBy,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        OperationalExecutionContext context,
        CancellationToken cancellationToken = default);

    Task FailAsync(
        OperationalExecutionContext context,
        string failureReason,
        bool isRetriable,
        CancellationToken cancellationToken = default);
}
