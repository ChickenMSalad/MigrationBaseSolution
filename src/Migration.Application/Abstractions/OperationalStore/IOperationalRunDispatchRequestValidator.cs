using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalRunDispatchRequestValidator
{
    void Validate(
        OperationalRunDispatchRequest request);
}
