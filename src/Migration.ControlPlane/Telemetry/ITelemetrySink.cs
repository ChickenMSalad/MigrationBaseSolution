namespace Migration.ControlPlane.Telemetry;

public interface ITelemetrySink
{
    TelemetryProviderDescriptor Descriptor { get; }

    Task<TelemetryWriteResult> WriteAsync(
        TelemetryEvent telemetryEvent,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryEvent>> QueryRecentAsync(
        string workspaceId,
        int take = 25,
        CancellationToken cancellationToken = default);
}
