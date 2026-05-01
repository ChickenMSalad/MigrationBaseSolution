using Migration.ControlPlane.Services;

namespace Migration.Admin.Api.Endpoints;

public sealed record ProjectCredentialBindingRequest(
    string? SourceCredentialSetId,
    string? TargetCredentialSetId);

public static class ProjectCredentialBindingEndpoints
{
    public static IEndpointRouteBuilder MapProjectCredentialBindingEndpoints(this IEndpointRouteBuilder app)
    {
        // This extension is called from Program.cs on the existing "/api" route group:
        //
        //     var api = app.MapGroup("/api");
        //     api.MapProjectCredentialBindingEndpoints();
        //
        // So this path must be relative to "/api".
        var group = app.MapGroup("/projects/{projectId}/credentials")
            .WithTags("Project Credentials");

        group.MapPut("", async (
            string projectId,
            ProjectCredentialBindingRequest request,
            IAdminProjectStore store,
            CancellationToken cancellationToken) =>
        {
            var existing = await store.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);

            if (existing is null)
            {
                return Results.NotFound(new { error = $"Project '{projectId}' was not found." });
            }

            if (existing.Settings is null)
            {
                return Results.BadRequest(new { error = "Project settings were not initialized." });
            }

            SetOrRemove(existing.Settings, "sourceCredentialSetId", request.SourceCredentialSetId);
            SetOrRemove(existing.Settings, "targetCredentialSetId", request.TargetCredentialSetId);

            await store.SaveProjectAsync(existing, cancellationToken).ConfigureAwait(false);

            return Results.Ok(existing);
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
