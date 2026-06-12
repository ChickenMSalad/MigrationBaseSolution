using Migration.ControlPlane.Telemetry;

namespace Migration.Admin.Api.Endpoints;

public static class TelemetryEventWriterEndpointExtensions
{
    public static RouteGroupBuilder MapTelemetryEventWriterEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapPost("/cloud/telemetry/writer/probe", async (
                ITelemetryEventWriter writer,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var request = new TelemetryEventWriteRequest(
                    WorkspaceId: workspaceId,
                    EventName: "telemetry.writer.probe",
                    Category: "diagnostics",
                    Severity: "information",
                    Dimensions: new Dictionary<string, string>
                    {
                        ["probe"] = "true",
                        ["source"] = "TelemetryEventWriterEndpointExtensions"
                    },
                    Metrics: new Dictionary<string, double>
                    {
                        ["probe.count"] = 1
                    });

                var result = await writer.WriteAsync(request, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    request,
                    result
                });
            })
            .WithName("ProbeTelemetryEventWriter")
            .WithTags("Cloud")
            .WithSummary("Writes a synthetic telemetry event through the telemetry event writer.");

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


