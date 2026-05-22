namespace Migration.ControlPlane.Telemetry;

public sealed record TelemetryEvent(
    string EventId,
    string WorkspaceId,
    string? TenantId,
    string EventName,
    string Category,
    string Severity,
    string CorrelationId,
    string? ProjectId,
    string? RunId,
    DateTimeOffset TimestampUtc,
    IReadOnlyDictionary<string, string> Dimensions,
    IReadOnlyDictionary<string, double> Metrics);

public sealed record TelemetryProviderDescriptor(
    string ProviderKind,
    bool IsConfigured,
    bool IsDurable,
    bool SupportsMetrics,
    bool SupportsTraces,
    bool SupportsCorrelation,
    IReadOnlyList<string> Warnings);

public sealed record TelemetryWriteResult(
    bool Accepted,
    string ProviderKind,
    string EventId,
    DateTimeOffset WrittenUtc);
