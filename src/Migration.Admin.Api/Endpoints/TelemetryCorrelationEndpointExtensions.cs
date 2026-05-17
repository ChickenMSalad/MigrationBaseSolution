using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class TelemetryCorrelationEndpointExtensions
{
    public static RouteGroupBuilder MapTelemetryCorrelationEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/telemetry/correlation", (
                IConfiguration configuration,
                IWebHostEnvironment environment,
                HttpContext httpContext) =>
            {
                var descriptor = BuildDescriptor(configuration, environment, httpContext);
                return Results.Ok(descriptor);
            })
            .WithName("GetTelemetryCorrelation")
            .WithTags("Cloud")
            .WithSummary("Gets safe telemetry/correlation conventions for cloud diagnostics.")
            .Produces<TelemetryCorrelationDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static TelemetryCorrelationDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        HttpContext httpContext)
    {
        var correlationId = FirstNonEmpty(
            httpContext.Request.Headers[TelemetryHeaderNames.CorrelationId].FirstOrDefault(),
            httpContext.TraceIdentifier,
            Guid.NewGuid().ToString("n"));

        var workspaceId = FirstNonEmptyOrNull(
            httpContext.Request.Headers[TelemetryHeaderNames.WorkspaceId].FirstOrDefault(),
            configuration["Workspace:WorkspaceId"]);

        var tenantId = FirstNonEmptyOrNull(
            httpContext.Request.Headers[TelemetryHeaderNames.TenantId].FirstOrDefault(),
            configuration["Workspace:TenantId"]);

        var telemetryMode = Read(
            configuration,
            "Cloud:TelemetryMode",
            InferTelemetryMode(configuration, environment));

        var appInsightsConfigured = IsConfigured(
            configuration["ApplicationInsights:ConnectionString"],
            configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"],
            configuration["ApplicationInsights:InstrumentationKey"])
            ? "true"
            : "false";

        var warnings = BuildWarnings(configuration, environment, telemetryMode, appInsightsConfigured);

        return new TelemetryCorrelationDescriptor(
            EnvironmentName: environment.EnvironmentName,
            CorrelationId: correlationId,
            RequestId: httpContext.TraceIdentifier,
            TraceIdentifier: httpContext.TraceIdentifier,
            WorkspaceId: workspaceId,
            TenantId: tenantId,
            TelemetryMode: telemetryMode,
            ApplicationInsightsConnectionConfigured: appInsightsConfigured,
            RecommendedHeaders:
            [
                TelemetryHeaderNames.CorrelationId,
                TelemetryHeaderNames.WorkspaceId,
                TelemetryHeaderNames.TenantId,
                TelemetryHeaderNames.RunId
            ],
            RecommendedLogProperties:
            [
                "correlationId",
                "requestId",
                "workspaceId",
                "tenantId",
                "runId",
                "projectId",
                "jobName",
                "queueMessageId",
                "leaseResource",
                "idempotencyKey"
            ],
            Warnings: warnings);
    }

    private static string InferTelemetryMode(IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (IsConfigured(
                configuration["ApplicationInsights:ConnectionString"],
                configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"],
                configuration["ApplicationInsights:InstrumentationKey"]))
        {
            return TelemetryModes.ApplicationInsights;
        }

        return environment.IsDevelopment()
            ? TelemetryModes.Console
            : TelemetryModes.Unknown;
    }

    private static List<string> BuildWarnings(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string telemetryMode,
        string appInsightsConfigured)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment() &&
            string.Equals(telemetryMode, TelemetryModes.Console, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should use Application Insights or OpenTelemetry.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(telemetryMode, TelemetryModes.Unknown, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Telemetry mode is not configured for a non-development environment.");
        }

        if (string.Equals(telemetryMode, TelemetryModes.ApplicationInsights, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(appInsightsConfigured, "true", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Application Insights mode is selected but no connection string/instrumentation key is configured.");
        }

        return warnings;
    }

    private static string Read(
        IConfiguration configuration,
        string key,
        string fallback)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool IsConfigured(params string?[] values) =>
        values.Any(value => !string.IsNullOrWhiteSpace(value));

    private static string? FirstNonEmptyOrNull(params string?[] values)
    {
        var value = FirstNonEmpty(values);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
