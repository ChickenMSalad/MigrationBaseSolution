using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalRunDispatchRequestHandler
{
    Task<OperationalRunDispatchResponse> HandleAsync(
        OperationalRunDispatchRequest request,
        CancellationToken cancellationToken = default);
}
