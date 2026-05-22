namespace Migration.ControlPlane.Telemetry;

public static class TelemetryEventFactory
{
    public static TelemetryEvent Create(
        string workspaceId,
        string eventName,
        string category,
        string severity = "information",
        string? tenantId = null,
        string? projectId = null,
        string? runId = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? dimensions = null,
        IReadOnlyDictionary<string, double>? metrics = null)
    {
        return new TelemetryEvent(
            EventId: Guid.NewGuid().ToString("N"),
            WorkspaceId: string.IsNullOrWhiteSpace(workspaceId) ? "default" : workspaceId,
            TenantId: tenantId,
            EventName: string.IsNullOrWhiteSpace(eventName) ? "unknown" : eventName,
            Category: string.IsNullOrWhiteSpace(category) ? "general" : category,
            Severity: string.IsNullOrWhiteSpace(severity) ? "information" : severity,
            CorrelationId: string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId,
            ProjectId: projectId,
            RunId: runId,
            TimestampUtc: DateTimeOffset.UtcNow,
            Dimensions: dimensions ?? new Dictionary<string, string>(),
            Metrics: metrics ?? new Dictionary<string, double>());
    }
}
