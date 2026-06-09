using Migration.ControlPlane.Storage;

namespace Migration.Admin.Api.Endpoints;

public static class ArtifactStorageBridgeEndpointExtensions
{
    private static readonly HashSet<string> AllowedKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ArtifactStorageKinds.Manifest,
        ArtifactStorageKinds.Mapping,
        ArtifactStorageKinds.Taxonomy,
        ArtifactStorageKinds.Other,
        ArtifactStorageKinds.Probe
    };

    public static RouteGroupBuilder MapArtifactStorageBridgeEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapPost("/cloud/artifacts/{artifactKind}/{artifactId}/files/{fileName}", async (
                string artifactKind,
                string artifactId,
                string fileName,
                HttpRequest request,
                HttpContext httpContext,
                IConfiguration configuration,
                IArtifactStorageService artifactStorage,
                IArtifactManifestIndexService manifestIndex,
                CancellationToken cancellationToken) =>
            {
                if (!AllowedKinds.Contains(artifactKind))
                {
                    return Results.BadRequest(new
                    {
                        error = "Unsupported artifact kind.",
                        artifactKind,
                        allowedKinds = AllowedKinds.OrderBy(x => x).ToArray()
                    });
                }

                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var contentType = FirstNonEmpty(
                    request.ContentType,
                    "application/octet-stream");

                var storageRequest = new ArtifactStorageRequest(
                    workspaceId,
                    artifactKind,
                    artifactId,
                    fileName,
                    contentType);

                var descriptor = await artifactStorage.WriteAsync(
                    storageRequest,
                    request.Body,
                    cancellationToken).ConfigureAwait(false);

                var index = await manifestIndex.AddAsync(
                    descriptor,
                    cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    artifact = descriptor,
                    index
                });
            })
            .WithName("UploadCloudArtifactBridge")
            .WithTags("Cloud")
            .WithSummary("Uploads an artifact file through the new artifact storage bridge.");

        api.MapGet("/cloud/artifacts/{artifactKind}/{artifactId}/files/{fileName}", async (
                string artifactKind,
                string artifactId,
                string fileName,
                HttpContext httpContext,
                IConfiguration configuration,
                IArtifactStorageService artifactStorage,
                CancellationToken cancellationToken) =>
            {
                if (!AllowedKinds.Contains(artifactKind))
                {
                    return Results.BadRequest(new
                    {
                        error = "Unsupported artifact kind.",
                        artifactKind,
                        allowedKinds = AllowedKinds.OrderBy(x => x).ToArray()
                    });
                }

                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var storageRequest = new ArtifactStorageRequest(
                    workspaceId,
                    artifactKind,
                    artifactId,
                    fileName,
                    "application/octet-stream");

                if (!await artifactStorage.ExistsAsync(storageRequest, cancellationToken).ConfigureAwait(false))
                {
                    return Results.NotFound(new
                    {
                        error = "Artifact file was not found.",
                        artifactKind,
                        artifactId,
                        fileName,
                        workspaceId
                    });
                }

                var stream = await artifactStorage.OpenReadAsync(
                    storageRequest,
                    cancellationToken).ConfigureAwait(false);

                return Results.File(
                    stream,
                    "application/octet-stream",
                    fileName);
            })
            .WithName("DownloadCloudArtifactBridge")
            .WithTags("Cloud")
            .WithSummary("Downloads an artifact file through the new artifact storage bridge.");

        api.MapDelete("/cloud/artifacts/{artifactKind}/{artifactId}/files/{fileName}", async (
                string artifactKind,
                string artifactId,
                string fileName,
                HttpContext httpContext,
                IConfiguration configuration,
                IArtifactStorageService artifactStorage,
                CancellationToken cancellationToken) =>
            {
                if (!AllowedKinds.Contains(artifactKind))
                {
                    return Results.BadRequest(new
                    {
                        error = "Unsupported artifact kind.",
                        artifactKind,
                        allowedKinds = AllowedKinds.OrderBy(x => x).ToArray()
                    });
                }

                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var storageRequest = new ArtifactStorageRequest(
                    workspaceId,
                    artifactKind,
                    artifactId,
                    fileName,
                    "application/octet-stream");

                await artifactStorage.DeleteAsync(
                    storageRequest,
                    cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    deleted = true,
                    artifactKind,
                    artifactId,
                    fileName,
                    workspaceId
                });
            })
            .WithName("DeleteCloudArtifactBridge")
            .WithTags("Cloud")
            .WithSummary("Deletes an artifact file through the new artifact storage bridge.");

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


