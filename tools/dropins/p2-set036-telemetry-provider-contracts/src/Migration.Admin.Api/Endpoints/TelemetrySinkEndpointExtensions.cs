using Migration.ControlPlane.Telemetry;

namespace Migration.Admin.Api.Endpoints;

public static class TelemetrySinkEndpointExtensions
{
    public static RouteGroupBuilder MapTelemetrySinkEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/telemetry/provider", (
                ITelemetrySink sink) =>
            Results.Ok(sink.Descriptor))
            .WithName("GetTelemetryProvider")
            .WithTags("Cloud")
            .WithSummary("Gets active telemetry provider diagnostics.");

        api.MapPost("/cloud/telemetry/probe", async (
                ITelemetrySink sink,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var telemetryEvent = TelemetryEventFactory.Create(
                    workspaceId: workspaceId,
                    eventName: "telemetry.probe",
                    category: "diagnostics",
                    severity: "information",
                    dimensions: new Dictionary<string, string>
                    {
                        ["probe"] = "true",
                        ["source"] = "TelemetrySinkEndpointExtensions"
                    },
                    metrics: new Dictionary<string, double>
                    {
                        ["probe.count"] = 1
                    });

                var result = await sink.WriteAsync(telemetryEvent, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    telemetryEvent,
                    result
                });
            })
            .WithName("ProbeTelemetry")
            .WithTags("Cloud")
            .WithSummary("Writes a synthetic telemetry event through the active sink.");

        api.MapGet("/cloud/telemetry/recent", async (
                ITelemetrySink sink,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var take = int.TryParse(httpContext.Request.Query["take"].FirstOrDefault(), out var parsed)
                    ? parsed
                    : 25;

                var events = await sink.QueryRecentAsync(workspaceId, take, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    workspaceId,
                    count = events.Count,
                    events
                });
            })
            .WithName("GetRecentTelemetry")
            .WithTags("Cloud")
            .WithSummary("Gets recent telemetry events from the active sink.");

        return api;
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
