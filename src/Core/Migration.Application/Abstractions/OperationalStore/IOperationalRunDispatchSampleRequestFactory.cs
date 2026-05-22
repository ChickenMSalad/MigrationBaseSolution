using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalRunDispatchSampleRequestFactory
{
    OperationalRunDispatchRequest CreateSample(
        int manifestRecordCount = 3);
}
