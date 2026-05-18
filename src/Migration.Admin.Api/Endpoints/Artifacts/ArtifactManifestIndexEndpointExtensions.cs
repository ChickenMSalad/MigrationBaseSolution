using System.Text;
using Migration.ControlPlane.Storage;

namespace Migration.Admin.Api.Endpoints;

public static class ArtifactManifestIndexEndpointExtensions
{
    public static RouteGroupBuilder MapArtifactManifestIndexEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/artifacts/index", async (
                IArtifactManifestIndexService indexService,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var index = await indexService.ReadAsync(workspaceId, cancellationToken).ConfigureAwait(false);
                return Results.Ok(index);
            })
            .WithName("GetArtifactManifestIndex")
            .WithTags("Cloud")
            .WithSummary("Gets the workspace artifact manifest index.");

        api.MapPost("/cloud/artifacts/index/probe", async (
                IArtifactStorageService artifactStorage,
                IArtifactManifestIndexService indexService,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var request = new ArtifactStorageRequest(
                    workspaceId,
                    ArtifactStorageKinds.Probe,
                    "manifest-index-probe",
                    "manifest-index-probe.txt",
                    "text/plain");

                await using var content = new MemoryStream(Encoding.UTF8.GetBytes(
                    $"artifact manifest index probe {DateTimeOffset.UtcNow:O}"));

                var artifact = await artifactStorage.WriteAsync(
                    request,
                    content,
                    cancellationToken).ConfigureAwait(false);

                var index = await indexService.AddAsync(artifact, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    artifact,
                    index
                });
            })
            .WithName("ProbeArtifactManifestIndex")
            .WithTags("Cloud")
            .WithSummary("Writes a probe artifact and records it in the artifact manifest index.");

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
