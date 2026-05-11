using Migration.ControlPlane.Models;
using Migration.ControlPlane.Services;
using Migration.Domain.Models;
using Migration.Orchestration.Preflight;

namespace Migration.Admin.Api.Endpoints;

public static class PreflightEndpointExtensions
{
    public static RouteGroupBuilder MapPreflightEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/preflight", async (
                MigrationJobDefinition job,
                IMigrationPreflightService preflight,
                CancellationToken cancellationToken) =>
            {
                var result = await preflight.RunAsync(new PreflightRequest(null, job), cancellationToken).ConfigureAwait(false);
                return Results.Ok(result);
            })
            .WithTags("Preflight")
            .WithSummary("Runs synchronous preflight validation for a supplied job definition without queueing a worker run.");

        //api.MapPost("/projects/{projectId}/preflight", async (
        //        string projectId,
        //        CreatePreflightRequest request,
        //        IAdminProjectStore store,
        //        ArtifactPathResolver artifactPathResolver,
        //        IMigrationPreflightService preflight,
        //        CancellationToken cancellationToken) =>
        //    {
        //        var project = await store.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
        //        if (project is null)
        //        {
        //            return Results.NotFound(new { error = $"Project '{projectId}' was not found." });
        //        }

        //        CreatePreflightRequest resolvedRequest;
        //        try
        //        {
        //            resolvedRequest = await artifactPathResolver.ResolvePreflightRequestAsync(project, request, cancellationToken).ConfigureAwait(false);
        //        }
        //        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
        //        {
        //            return Results.BadRequest(new { error = ex.Message });
        //        }

        //        if (string.IsNullOrWhiteSpace(resolvedRequest.ManifestPath) || string.IsNullOrWhiteSpace(resolvedRequest.MappingProfilePath))
        //        {
        //            return Results.BadRequest(new { error = "ManifestPath and MappingProfilePath are required. Bind artifacts to the project or supply path overrides." });
        //        }

        //        var jobName = string.IsNullOrWhiteSpace(resolvedRequest.JobName)
        //            ? $"{project.ProjectId}-preflight-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"
        //            : resolvedRequest.JobName.Trim();

        //        var settings = MergeSettings(project.Settings, resolvedRequest.Settings);
        //        var validateSourceSample = GetBoolean(settings, "Preflight:ValidateSourceSample") || GetBoolean(settings, "ValidateSourceSample");
        //        var sourceSampleSize = GetInt(settings, "Preflight:SourceSampleSize", 0);
        //        var maxRows = GetInt(settings, "Preflight:MaxRows", 250);

        //        var job = new MigrationJobDefinition
        //        {
        //            JobName = jobName,
        //            SourceType = project.SourceType,
        //            TargetType = project.TargetType,
        //            ManifestType = project.ManifestType,
        //            ManifestPath = resolvedRequest.ManifestPath,
        //            MappingProfilePath = resolvedRequest.MappingProfilePath,
        //            DryRun = true,
        //            Parallelism = 1,
        //            Settings = settings
        //        };

        //        var result = await preflight.RunAsync(new PreflightRequest(project.ProjectId, job, maxRows, validateSourceSample, sourceSampleSize), cancellationToken).ConfigureAwait(false);
        //        return Results.Ok(result);
        //    })
        //    .WithTags("Preflight")
        //    .WithSummary("Runs synchronous preflight validation for a project using bound artifacts or path overrides.");

        return api;
    }

    private static Dictionary<string, string?> MergeSettings(Dictionary<string, string?> projectSettings, Dictionary<string, string?>? requestSettings)
    {
        var merged = new Dictionary<string, string?>(projectSettings, StringComparer.OrdinalIgnoreCase);
        if (requestSettings is not null)
        {
            foreach (var pair in requestSettings)
            {
                merged[pair.Key] = pair.Value;
            }
        }
        return merged;
    }

    private static bool GetBoolean(Dictionary<string, string?> settings, string key)
        => settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed) && parsed;

    private static int GetInt(Dictionary<string, string?> settings, string key, int fallback)
        => settings.TryGetValue(key, out var raw) && int.TryParse(raw, out var parsed) ? parsed : fallback;
}
