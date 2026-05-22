using Migration.ControlPlane.Models;
using Migration.ControlPlane.Services;

namespace Migration.Admin.Api.Endpoints;

public static class ProjectArtifactBindingEndpoints
{
    public static IEndpointRouteBuilder MapProjectArtifactBindingEndpoints(this IEndpointRouteBuilder app)
    {
        // This extension is called from Program.cs on the existing "/api" route group:
        //
        //     var api = app.MapGroup("/api");
        //     api.MapProjectArtifactBindingEndpoints();
        //
        // So this path must be relative to "/api".
        // Do not include another "/api" prefix here.
        var group = app.MapGroup("/projects/{projectId}/artifacts")
            .WithTags("Project Artifacts");

        group.MapGet("", async (
            string projectId,
            IAdminProjectStore store,
            ArtifactPathResolver resolver,
            CancellationToken cancellationToken) =>
        {
            var project = await store.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);

            if (project is null)
            {
                return Results.NotFound(new { error = $"Project '{projectId}' was not found." });
            }

            var binding = await resolver.GetBindingAsync(project, cancellationToken).ConfigureAwait(false);

            return Results.Ok(binding);
        })
        .WithSummary("Get the manifest/mapping artifact binding for a project.");

        group.MapPut("", async (
            string projectId,
            ProjectArtifactBindingRequest request,
            IAdminProjectStore store,
            AdminRunFactory factory,
            ArtifactPathResolver resolver,
            CancellationToken cancellationToken) =>
        {
            var existing = await store.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);

            if (existing is null)
            {
                return Results.NotFound(new { error = $"Project '{projectId}' was not found." });
            }

            var updated = factory.BindArtifacts(existing, request);

            // Validate artifact ids immediately so the UI fails at bind time,
            // not only at run time.
            await resolver.GetBindingAsync(updated, cancellationToken).ConfigureAwait(false);

            await store.SaveProjectAsync(updated, cancellationToken).ConfigureAwait(false);

            return Results.Ok(updated);
        })
        .WithSummary("Bind uploaded manifest/mapping artifacts to a project.");

        return app;
    }
}
