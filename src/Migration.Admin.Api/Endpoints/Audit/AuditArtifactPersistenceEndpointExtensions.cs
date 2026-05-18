using Migration.ControlPlane.Audit;

namespace Migration.Admin.Api.Endpoints;

public static class AuditArtifactPersistenceEndpointExtensions
{
    public static RouteGroupBuilder MapAuditArtifactPersistenceEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/audit/artifact-persistence/configuration", (
                IConfiguration configuration) =>
            {
                return Results.Ok(new
                {
                    provider = configuration["Audit:Provider"] ?? "InMemory",
                    artifactKind = configuration["Audit:ArtifactKind"] ?? "audit",
                    artifactId = configuration["Audit:ArtifactId"] ?? "events",
                    fileNamePrefix = configuration["Audit:FileNamePrefix"] ?? "audit-event",
                    recentQueryLimit = configuration["Audit:RecentQueryLimit"] ?? "100"
                });
            })
            .WithName("GetAuditArtifactPersistenceConfiguration")
            .WithTags("Cloud")
            .WithSummary("Gets audit artifact persistence configuration.");

        return api;
    }
}
