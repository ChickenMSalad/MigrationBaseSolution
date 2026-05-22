using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalRunDispatchCommandService
{
    Task<OperationalRunDispatchResult> DispatchAsync(
        OperationalRunDispatchCommand command,
        CancellationToken cancellationToken = default);
}
