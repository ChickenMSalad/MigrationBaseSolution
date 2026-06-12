namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Safe telemetry/correlation descriptor for cloud operations.
/// This does not expose secrets and does not require a telemetry provider package.
/// </summary>
public sealed record TelemetryCorrelationDescriptor(
    string EnvironmentName,
    string CorrelationId,
    string RequestId,
    string TraceIdentifier,
    string? WorkspaceId,
    string? TenantId,
    string TelemetryMode,
    string? ApplicationInsightsConnectionConfigured,
    IReadOnlyList<string> RecommendedHeaders,
    IReadOnlyList<string> RecommendedLogProperties,
    IReadOnlyList<string> Warnings);

public static class TelemetryModes
{
    public const string Console = "console";
    public const string ApplicationInsights = "applicationInsights";
    public const string OpenTelemetry = "openTelemetry";
    public const string Unknown = "unknown";
}

public static class TelemetryHeaderNames
{
    public const string CorrelationId = "X-Correlation-Id";
    public const string WorkspaceId = "X-Workspace-Id";
    public const string TenantId = "X-Tenant-Id";
    public const string RunId = "X-Run-Id";
}


