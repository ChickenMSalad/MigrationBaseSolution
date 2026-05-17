namespace Migration.ControlPlane.Audit;

public static class QueueAuditEventNames
{
    public const string DispatchAccepted = "queue.dispatch.accepted";
    public const string ReceivePolled = "queue.receive.polled";
    public const string MessagePlanned = "queue.message.planned";
    public const string MessageFailed = "queue.message.failed";
    public const string FailureArtifactWritten = "queue.failure-artifact.written";
    public const string CoordinatorPolled = "queue.coordinator.polled";
}

public static class AuditCategories
{
    public const string Queue = "queue";
    public const string Diagnostics = "diagnostics";
    public const string Cloud = "cloud";
}
