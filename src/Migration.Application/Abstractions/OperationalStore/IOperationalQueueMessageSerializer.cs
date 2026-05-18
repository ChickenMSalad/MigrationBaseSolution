using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalQueueMessageSerializer
{
    string Serialize(
        OperationalQueueMessage message);

    OperationalQueueMessage Deserialize(
        string payload);

    bool TryDeserialize(
        string payload,
        out OperationalQueueMessage? message);
}
