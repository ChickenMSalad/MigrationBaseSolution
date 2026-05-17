namespace Migration.ControlPlane.Telemetry;

public static class QueueTelemetryEventNames
{
    public const string DispatchAccepted = "queue.dispatch.accepted";
    public const string ReceivePolled = "queue.receive.polled";
    public const string MessagePlanned = "queue.message.planned";
    public const string MessageFailed = "queue.message.failed";
    public const string CoordinatorPolled = "queue.coordinator.polled";
}

public static class TelemetryCategories
{
    public const string Queue = "queue";
    public const string Diagnostics = "diagnostics";
    public const string Cloud = "cloud";
}
