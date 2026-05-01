using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Migration.Admin.Api.Endpoints;
using Migration.ControlPlane.Models;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Registration;
using Migration.ControlPlane.Services;
using Migration.GenericRuntime.Registration;
using Migration.Orchestration.Abstractions;
using Migration.Orchestration.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables(prefix: "MIGRATION_");

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"ControlPlane:StorageRoot = {builder.Configuration["ControlPlane:StorageRoot"]}");
Console.WriteLine($"MigrationRunQueue:Provider = {builder.Configuration["MigrationRunQueue:Provider"]}");
Console.WriteLine($"MigrationRunQueue:QueueName = {builder.Configuration["MigrationRunQueue:QueueName"]}");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Migration Admin API",
        Version = "v1",
        Description = "Control-plane API for connector discovery, migration project setup, preflight requests, run queueing, and run status."
    });
});

builder.Services.AddMigrationOrchestration(builder.Configuration);
builder.Services.AddGenericMigrationRuntime(builder.Configuration);
builder.Services.AddMigrationControlPlane(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Migration Admin API v1");
    options.DocumentTitle = "Migration Admin API";
});

app.MapGet("/", () => Results.Redirect("/swagger"))
    .WithName("Root")
    .ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "Migration.Admin.Api",
    utc = DateTimeOffset.UtcNow
}))
.WithName("Health")
.WithTags("Health")
.WithSummary("Returns API health information.");

var api = app.MapGroup("/api");

// Extensions below are relative to the existing /api route group.
api.MapRunMonitoringEndpoints();
api.MapCredentialEndpoints();
api.MapProjectArtifactBindingEndpoints();
api.MapProjectCredentialBindingEndpoints();
api.MapPreflightEndpoints();

api.MapGet("/connectors", (IConnectorCatalog catalog) => Results.Ok(new
{
    sources = catalog.GetSources(),
    targets = catalog.GetTargets(),
    manifestProviders = catalog.GetManifestProviders()
}))
.WithName("GetConnectorCatalog")
.WithTags("Connectors")
.WithSummary("Gets all registered source, target, and manifest connector descriptors.");

api.MapGet("/connectors/sources", (IConnectorCatalog catalog) => Results.Ok(catalog.GetSources()))
    .WithName("GetSourceConnectors")
    .WithTags("Connectors")
    .WithSummary("Gets source connector descriptors.");

api.MapGet("/connectors/targets", (IConnectorCatalog catalog) => Results.Ok(catalog.GetTargets()))
    .WithName("GetTargetConnectors")
    .WithTags("Connectors")
    .WithSummary("Gets target connector descriptors.");

api.MapGet("/connectors/manifests", (IConnectorCatalog catalog) => Results.Ok(catalog.GetManifestProviders()))
    .WithName("GetManifestProviders")
    .WithTags("Connectors")
    .WithSummary("Gets manifest provider descriptors.");

api.MapGet("/manifest-providers", (IConnectorCatalog catalog) => Results.Ok(catalog.GetManifestProviders()))
    .WithName("GetManifestProvidersLegacy")
    .WithTags("Connectors")
    .WithSummary("Legacy alias for manifest provider descriptors.");

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

api.MapPost("/projects", async (CreateMigrationProjectRequest request, AdminRunFactory factory, IAdminProjectStore store, CancellationToken cancellationToken) =>
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

api.MapPut("/projects/{projectId}", async (string projectId, UpdateMigrationProjectRequest request, AdminRunFactory factory, IAdminProjectStore store, CancellationToken cancellationToken) =>
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

api.MapPost("/projects/{projectId}/preflight", async (string projectId, CreatePreflightRequest request, AdminRunFactory factory, IAdminProjectStore store, IMigrationRunQueue queue, [FromServices] ArtifactPathResolver artifactPathResolver, CancellationToken cancellationToken) =>
{
    var project = await store.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
    if (project is null)
    {
        return Results.NotFound(new { error = $"Project '{projectId}' was not found." });
    }

    CreatePreflightRequest resolvedRequest;
    try
    {
        resolvedRequest = await artifactPathResolver.ResolvePreflightRequestAsync(project, request, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    var run = factory.CreatePreflight(project, resolvedRequest);
    await store.SaveRunAsync(run, cancellationToken).ConfigureAwait(false);
    await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false);
    return Results.Accepted($"/api/runs/{run.RunId}", run);
})
.WithName("QueuePreflight")
.WithTags("Runs")
.WithSummary("Queues a preflight-only run for a project.");

api.MapPost("/projects/{projectId}/runs", async (string projectId, CreateRunRequest request, AdminRunFactory factory, IAdminProjectStore store, IMigrationRunQueue queue, [FromServices] ArtifactPathResolver artifactPathResolver, CancellationToken cancellationToken) =>
{
    var project = await store.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
    if (project is null)
    {
        return Results.NotFound(new { error = $"Project '{projectId}' was not found." });
    }
    CreateRunRequest resolvedRequest;
    try
    {
        resolvedRequest = await artifactPathResolver.ResolveRunRequestAsync(project, request, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    var run = factory.CreateRun(project, resolvedRequest);
    await store.SaveRunAsync(run, cancellationToken).ConfigureAwait(false);
    await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false);
    return Results.Accepted($"/api/runs/{run.RunId}", run);
})
.WithName("QueueRun")
.WithTags("Runs")
.WithSummary("Queues a migration run for a project.");

api.MapGet("/runs", async (IAdminProjectStore store, CancellationToken cancellationToken) =>
    Results.Ok(await store.ListRunsAsync(cancellationToken).ConfigureAwait(false)))
    .WithName("GetRuns")
    .WithTags("Runs")
    .WithSummary("Lists migration runs.");

api.MapGet("/runs/{runId}", async (string runId, IAdminProjectStore store, CancellationToken cancellationToken) =>
{
    var run = await store.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
    return run is null ? Results.NotFound() : Results.Ok(run);
})
.WithName("GetRun")
.WithTags("Runs")
.WithSummary("Gets a migration run by id.");

api.MapPost("/runs/{runId}/cancel", async (string runId, IAdminProjectStore store, CancellationToken cancellationToken) =>
{
    var run = await store.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
    if (run is null)
    {
        return Results.NotFound();
    }

    if (run.Status is AdminRunStatuses.Completed or AdminRunStatuses.Failed or AdminRunStatuses.Canceled)
    {
        return Results.Conflict(new { error = $"Run is already terminal: {run.Status}." });
    }

    var canceled = run with
    {
        Status = AdminRunStatuses.Canceled,
        UpdatedUtc = DateTimeOffset.UtcNow,
        CompletedUtc = DateTimeOffset.UtcNow,
        Message = "Run was canceled from the Admin API control record. Running worker cancellation is cooperative and handled by future cancellation tokens/leases."
    };

    await store.SaveRunAsync(canceled, cancellationToken).ConfigureAwait(false);
    return Results.Ok(canceled);
})
.WithName("CancelRun")
.WithTags("Runs")
.WithSummary("Marks a queued or running migration run as canceled in the control-plane store.");

api.MapGet("/runs/{runId}/work-items", async (string runId, IAdminProjectStore store, IMigrationExecutionStateMaintenance state, CancellationToken cancellationToken) =>
{
    var run = await store.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
    if (run is null)
    {
        return Results.NotFound();
    }

    var items = await state.ListWorkItemsAsync(run.JobName, cancellationToken).ConfigureAwait(false);
    var matching = items
        .Where(x => string.Equals(x.RunId, run.RunId, StringComparison.OrdinalIgnoreCase) || string.Equals(x.JobName, run.JobName, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(x => x.UpdatedUtc)
        .ToList();

    return Results.Ok(new RunWorkItemsResponse(run.RunId, run.JobName, matching.Count, matching));
})
.WithName("GetRunWorkItems")
.WithTags("Runs")
.WithSummary("Lists state-store work items associated with a migration run.");

app.MapGet("/debug/config", (IConfiguration configuration, IWebHostEnvironment env) =>
{
    return Results.Json(new
    {
        Environment = env.EnvironmentName,
        ContentRoot = env.ContentRootPath,
        ControlPlaneStorageRoot = configuration["ControlPlane:StorageRoot"],
        QueueProvider = configuration["MigrationRunQueue:Provider"],
        QueueName = configuration["MigrationRunQueue:QueueName"]
    });
});

// These extensions already include their /api route prefix internally.
// Keep them on app, not on the /api group, to avoid /api/api routes.
app.MapArtifactEndpoints();
app.MapControlPlaneDeleteEndpoints();
app.MapMappingBuilderEndpoints();
app.MapManifestBuilderEndpoints();

app.Run();
