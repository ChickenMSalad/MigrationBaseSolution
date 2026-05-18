using System.Text;
using Migration.ControlPlane.Storage;

namespace Migration.Admin.Api.Endpoints;

public static class CloudBinaryStorageProbeEndpointExtensions
{
    public static RouteGroupBuilder MapCloudBinaryStorageProbeEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/storage/provider", (
                CloudBinaryStorageProviderCapabilities capabilities) =>
            Results.Ok(capabilities))
            .WithName("GetCloudBinaryStorageProvider")
            .WithTags("Cloud")
            .WithSummary("Gets the active binary storage provider capabilities.");

        api.MapPost("/cloud/storage/probe", async (
                ICloudStoragePathResolver resolver,
                ICloudBinaryStorageProvider provider,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var probeRoot = resolver.ResolveArtifactRoot(workspaceId, "probe");
                var probeLocation = probeRoot with
                {
                    RelativePath = $"{probeRoot.RelativePath}/storage-probe.txt",
                    Uri = $"{probeRoot.Uri.TrimEnd('/', '\\')}/storage-probe.txt"
                };

                await using var content = new MemoryStream(Encoding.UTF8.GetBytes(
                    $"storage probe {DateTimeOffset.UtcNow:O}"));

                await provider.WriteAsync(
                    probeLocation,
                    content,
                    "text/plain",
                    cancellationToken).ConfigureAwait(false);

                var exists = await provider.ExistsAsync(probeLocation, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    workspaceId,
                    exists,
                    location = probeLocation
                });
            })
            .WithName("ProbeCloudBinaryStorage")
            .WithTags("Cloud")
            .WithSummary("Writes a small probe object through the active binary storage provider.");

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
