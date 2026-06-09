using System.Text;
using Migration.ControlPlane.Storage;

namespace Migration.Admin.Api.Endpoints;

public static class ArtifactStorageProbeEndpointExtensions
{
    public static RouteGroupBuilder MapArtifactStorageProbeEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/artifacts/resolve", (
                IArtifactStorageService artifactStorage,
                HttpContext httpContext,
                IConfiguration configuration) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var artifactKind = FirstNonEmpty(
                    httpContext.Request.Query["kind"].FirstOrDefault(),
                    ArtifactStorageKinds.Manifest);

                var artifactId = FirstNonEmpty(
                    httpContext.Request.Query["artifactId"].FirstOrDefault(),
                    "sample-artifact");

                var fileName = FirstNonEmpty(
                    httpContext.Request.Query["fileName"].FirstOrDefault(),
                    "sample.json");

                var descriptor = artifactStorage.Resolve(new ArtifactStorageRequest(
                    workspaceId,
                    artifactKind,
                    artifactId,
                    fileName,
                    "application/json"));

                return Results.Ok(descriptor);
            })
            .WithName("ResolveCloudArtifactStorage")
            .WithTags("Cloud")
            .WithSummary("Resolves a logical artifact object location through the artifact storage service.");

        api.MapPost("/cloud/artifacts/probe", async (
                IArtifactStorageService artifactStorage,
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
                    "storage-probe",
                    "artifact-storage-probe.txt",
                    "text/plain");

                await using var content = new MemoryStream(Encoding.UTF8.GetBytes(
                    $"artifact storage probe {DateTimeOffset.UtcNow:O}"));

                var descriptor = await artifactStorage.WriteAsync(
                    request,
                    content,
                    cancellationToken).ConfigureAwait(false);

                var exists = await artifactStorage.ExistsAsync(request, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    exists,
                    artifact = descriptor
                });
            })
            .WithName("ProbeCloudArtifactStorage")
            .WithTags("Cloud")
            .WithSummary("Writes a small probe artifact through the artifact storage service.");

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


