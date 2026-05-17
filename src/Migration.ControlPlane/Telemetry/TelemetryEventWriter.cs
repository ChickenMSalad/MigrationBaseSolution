namespace Migration.ControlPlane.Telemetry;

public sealed class TelemetryEventWriter : ITelemetryEventWriter
{
    private readonly ITelemetrySink _sink;

    public TelemetryEventWriter(ITelemetrySink sink)
    {
        _sink = sink;
    }

    public Task<TelemetryWriteResult> WriteAsync(
        TelemetryEventWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var telemetryEvent = TelemetryEventFactory.Create(
            workspaceId: request.WorkspaceId,
            eventName: request.EventName,
            category: request.Category,
            severity: request.Severity,
            tenantId: request.TenantId,
            projectId: request.ProjectId,
            runId: request.RunId,
            correlationId: request.CorrelationId,
            dimensions: request.Dimensions,
            metrics: request.Metrics);

        return _sink.WriteAsync(telemetryEvent, cancellationToken);
    }
}
