using Migration.ControlPlane.Services;

namespace Migration.Admin.Api.Endpoints;

public static class ProjectCredentialBindingEndpoints
{
    public static IEndpointRouteBuilder MapProjectCredentialBindingEndpoints(this IEndpointRouteBuilder app)
    {
        // Called from Program.cs on the existing /api route group:
        //
        //     var api = app.MapGroup("/api");
        //     api.MapProjectCredentialBindingEndpoints();
        //
        // Therefore this path must be relative to /api.
        var group = app.MapGroup("/projects/{projectId}/credentials")
            .WithTags("Project Credentials");

        group.MapPut("", async (
            string projectId,
            ProjectCredentialBindingRequest request,
            IAdminProjectStore store,
            CancellationToken cancellationToken) =>
        {
            var project = await store.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);

            if (project is null)
            {
                return Results.NotFound(new { error = $"Project '{projectId}' was not found." });
            }

            // Settings is the existing persisted project context bag in this repo.
            // It is init-only on the project record, so do not assign project.Settings.
            // Mutate the existing dictionary instead.
            if (project.Settings is null)
            {
                return Results.BadRequest(new { error = "Project settings were not initialized." });
            }

            SetOrRemove(project.Settings, "sourceCredentialSetId", request.SourceCredentialSetId);
            SetOrRemove(project.Settings, "targetCredentialSetId", request.TargetCredentialSetId);

            await store.SaveProjectAsync(project, cancellationToken).ConfigureAwait(false);

            return Results.Ok(project);
        })
        .WithSummary("Bind source and target credential set ids to a project.");

        return app;
    }

    private static void SetOrRemove(IDictionary<string, string?> settings, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            settings.Remove(key);
            return;
        }

        settings[key] = value.Trim();
    }
}

public sealed record ProjectCredentialBindingRequest(
    string? SourceCredentialSetId,
    string? TargetCredentialSetId);
