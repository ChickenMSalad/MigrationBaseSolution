using Migration.ControlPlane.Audit;

namespace Migration.Admin.Api.Endpoints;

public static class AuditPersistenceEndpointExtensions
{
    public static RouteGroupBuilder MapAuditPersistenceEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/audit/persistence/provider", (
                IAuditPersistenceProvider provider) =>
            Results.Ok(provider.Descriptor))
            .WithName("GetAuditPersistenceProvider")
            .WithTags("Cloud")
            .WithSummary("Gets active audit persistence provider diagnostics.");

        api.MapPost("/cloud/audit/persistence/probe", async (
                IAuditPersistenceProvider provider,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var record = AuditRecordFactory.Create(
                    workspaceId: workspaceId,
                    category: "diagnostics",
                    eventName: "audit.persistence.probe",
                    severity: "information",
                    actor: "admin-api",
                    properties: new Dictionary<string, string>
                    {
                        ["probe"] = "true",
                        ["source"] = "AuditPersistenceEndpointExtensions"
                    });

                var result = await provider.WriteAsync(record, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    record,
                    result
                });
            })
            .WithName("ProbeAuditPersistence")
            .WithTags("Cloud")
            .WithSummary("Writes a synthetic audit record through the active audit persistence provider.");

        api.MapGet("/cloud/audit/persistence/recent", async (
                IAuditPersistenceProvider provider,
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

                var records = await provider.QueryRecentAsync(workspaceId, take, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    workspaceId,
                    count = records.Count,
                    records
                });
            })
            .WithName("GetRecentAuditRecords")
            .WithTags("Cloud")
            .WithSummary("Gets recent audit records from the active audit persistence provider.");

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


