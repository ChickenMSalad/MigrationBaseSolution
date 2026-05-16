using Migration.ControlPlane.Models;
using Migration.ControlPlane.Services;

namespace Migration.Admin.Api.Endpoints;

public static class ProjectEndpointExtensions
{
    public static RouteGroupBuilder MapProjectEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/projects", async (IAdminProjectStore store, CancellationToken cancellationToken) =>
                Results.Ok((await store.ListProjectsAsync(cancellationToken).ConfigureAwait(false)).OrderByDescending(x => x.UpdatedUtc)))
            .WithName("GetProjects")
            .WithTags("Projects")
            .WithSummary("Lists migration projects stored in the control-plane store.");

        api.MapGet("/projects/{projectId}", async (string projectId, IAdminProjectStore store, CancellationToken cancellationToken) =>
            {
                var project = await store.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
                return project is null ? Results.NotFound() : Results.Ok(project);
            })
            .WithName("GetProject")
            .WithTags("Projects")
            .WithSummary("Gets a migration project by id.");

        api.MapPost("/projects", async (
                CreateMigrationProjectRequest request,
                AdminRunFactory factory,
                IAdminProjectStore store,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.DisplayName) ||
                    string.IsNullOrWhiteSpace(request.SourceType) ||
                    string.IsNullOrWhiteSpace(request.TargetType) ||
                    string.IsNullOrWhiteSpace(request.ManifestType))
                {
                    return Results.BadRequest(new { error = "DisplayName, SourceType, TargetType, and ManifestType are required." });
                }

                var project = factory.CreateProject(request);
                await store.SaveProjectAsync(project, cancellationToken).ConfigureAwait(false);
                return Results.Created($"/api/projects/{project.ProjectId}", project);
            })
            .WithName("CreateProject")
            .WithTags("Projects")
            .WithSummary("Creates a migration project definition.");

        api.MapPut("/projects/{projectId}", async (
                string projectId,
                UpdateMigrationProjectRequest request,
                AdminRunFactory factory,
                IAdminProjectStore store,
                CancellationToken cancellationToken) =>
            {
                var existing = await store.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
                if (existing is null)
                {
                    return Results.NotFound();
                }

                var updated = factory.UpdateProject(existing, request);
                await store.SaveProjectAsync(updated, cancellationToken).ConfigureAwait(false);
                return Results.Ok(updated);
            })
            .WithName("UpdateProject")
            .WithTags("Projects")
            .WithSummary("Updates a migration project definition.");

        api.MapDelete("/projects/{projectId}", async (string projectId, IAdminProjectStore store, CancellationToken cancellationToken) =>
                await store.DeleteProjectAsync(projectId, cancellationToken).ConfigureAwait(false) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteProject")
            .WithTags("Projects")
            .WithSummary("Deletes a migration project definition.");

        return api;
    }
}
