using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalExecutionContextFactory
{
    Task<OperationalExecutionContext?> CreateAsync(
        Guid workItemId,
        CancellationToken cancellationToken = default);
}
