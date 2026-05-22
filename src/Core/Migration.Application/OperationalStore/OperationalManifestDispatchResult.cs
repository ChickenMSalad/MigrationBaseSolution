using Migration.Application.Models.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalManifestDispatchResult
{
    public OperationalManifestDispatchResult(
        MigrationManifestRecord manifestRecord,
        OperationalQueueMessage? queueMessage)
    {
        ManifestRecord = manifestRecord;
        QueueMessage = queueMessage;
    }

    public MigrationManifestRecord ManifestRecord { get; }

    public OperationalQueueMessage? QueueMessage { get; }
}
