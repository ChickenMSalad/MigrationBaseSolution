using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalExecutionContextFactory
{
    Task<OperationalExecutionContext?> CreateAsync(
        long workItemId,
        CancellationToken cancellationToken = default);
}
