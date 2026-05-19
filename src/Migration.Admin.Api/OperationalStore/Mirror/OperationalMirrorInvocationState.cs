namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorInvocationState
{
    private readonly object _syncRoot = new();

    private OperationalMirrorInvocationSnapshot _snapshot = OperationalMirrorInvocationSnapshot.Empty;

    public void RecordSkipped(
        string legacyRunId,
        string reason)
    {
        Record(
            legacyRunId,
            invoked: true,
            mirrored: false,
            failed: false,
            message: reason);
    }

    public void RecordMirrored(
        string legacyRunId,
        Guid operationalRunId)
    {
        Record(
            legacyRunId,
            invoked: true,
            mirrored: true,
            failed: false,
            message: $"Mirrored as operational run {operationalRunId}.");
    }

    public void RecordFailed(
        string legacyRunId,
        Exception exception)
    {
        Record(
            legacyRunId,
            invoked: true,
            mirrored: false,
            failed: true,
            message: exception.Message);
    }

    private void Record(
        string legacyRunId,
        bool invoked,
        bool mirrored,
        bool failed,
        string message)
    {
        lock (_syncRoot)
        {
            _snapshot = new OperationalMirrorInvocationSnapshot
            {
                Invoked = invoked,
                Mirrored = mirrored,
                Failed = failed,
                LegacyRunId = legacyRunId,
                Message = message,
                RecordedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public OperationalMirrorInvocationSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            return _snapshot;
        }
    }
}
