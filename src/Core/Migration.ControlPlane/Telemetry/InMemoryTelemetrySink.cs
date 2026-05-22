namespace Migration.ControlPlane.Telemetry;

public sealed class InMemoryTelemetrySink : ITelemetrySink
{
    private readonly object _gate = new();
    private readonly List<TelemetryEvent> _events = [];

    public TelemetryProviderDescriptor Descriptor { get; } = new(
        ProviderKind: "inMemory",
        IsConfigured: true,
        IsDurable: false,
        SupportsMetrics: true,
        SupportsTraces: true,
        SupportsCorrelation: true,
        Warnings:
        [
            "In-memory telemetry is diagnostics-only and does not survive process restarts."
        ]);

    public Task<TelemetryWriteResult> WriteAsync(
        TelemetryEvent telemetryEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);

        lock (_gate)
        {
            _events.Add(telemetryEvent);
        }

        return Task.FromResult(new TelemetryWriteResult(
            Accepted: true,
            ProviderKind: Descriptor.ProviderKind,
            EventId: telemetryEvent.EventId,
            WrittenUtc: DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyList<TelemetryEvent>> QueryRecentAsync(
        string workspaceId,
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var events = _events
                .Where(x => string.Equals(x.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.TimestampUtc)
                .Take(Math.Clamp(take, 1, 250))
                .ToArray();

            return Task.FromResult<IReadOnlyList<TelemetryEvent>>(events);
        }
    }
}
