namespace Migration.ControlPlane.Telemetry;

public interface ITelemetryEventWriter
{
    Task<TelemetryWriteResult> WriteAsync(
        TelemetryEventWriteRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record TelemetryEventWriteRequest(
    string WorkspaceId,
    string EventName,
    string Category,
    string Severity = "information",
    string? TenantId = null,
    string? ProjectId = null,
    string? RunId = null,
    string? CorrelationId = null,
    IReadOnlyDictionary<string, string>? Dimensions = null,
    IReadOnlyDictionary<string, double>? Metrics = null);
