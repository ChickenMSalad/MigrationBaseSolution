using Migration.ControlPlane.Audit;

namespace Migration.Admin.Api.Endpoints;

public static class AuditEventWriterEndpointExtensions
{
    public static RouteGroupBuilder MapAuditEventWriterEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapPost("/cloud/audit/writer/probe", async (
                IAuditEventWriter writer,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var request = new AuditEventWriteRequest(
                    WorkspaceId: workspaceId,
                    Category: "diagnostics",
                    EventName: "audit.writer.probe",
                    Severity: "information",
                    Actor: "admin-api",
                    Properties: new Dictionary<string, string>
                    {
                        ["probe"] = "true",
                        ["source"] = "AuditEventWriterEndpointExtensions"
                    });

                var result = await writer.WriteAsync(request, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    request,
                    result
                });
            })
            .WithName("ProbeAuditEventWriter")
            .WithTags("Cloud")
            .WithSummary("Writes a synthetic audit event through the audit event writer.");

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
