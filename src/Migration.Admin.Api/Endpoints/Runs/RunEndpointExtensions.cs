using Microsoft.AspNetCore.Mvc;
using Migration.Admin.Api.Contracts;
using Migration.ControlPlane.Models;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Services;
using Migration.Orchestration.Abstractions;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class RunEndpointExtensions
{
    private const string RunPreflightRequiredAction =
        "Run project preflight and wait for it to complete successfully before starting a non-dry-run migration.";

    public static RouteGroupBuilder MapRunEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapPost("/projects/{projectId}/preflight", async (
                string projectId,
                CreatePreflightRequest request,
                AdminRunFactory factory,
                IAdminProjectStore store,
                IAdminOperationalRunMirrorService operationalRunMirror,
                IMigrationRunQueue queue,
                [FromServices] ArtifactPathResolver artifactPathResolver,
                CancellationToken cancellationToken) =>
            {
                var project = await store.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
                if (project is null)
                {
                    return Results.NotFound(new AdminApiErrorResponse($"Project '{projectId}' was not found."));
                }

                CreatePreflightRequest resolvedRequest;
                try
                {
                    resolvedRequest = await artifactPathResolver
                        .ResolvePreflightRequestAsync(project, request, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
                {
                    return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                }

                var run = factory.CreatePreflight(project, resolvedRequest);

                await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false);
                await operationalRunMirror.MirrorRunAsync(project, run, cancellationToken).ConfigureAwait(false);
                await store.SaveRunAsync(run, cancellationToken).ConfigureAwait(false);

                return Results.Accepted($"/api/runs/{run.RunId}", run);
            })
            .WithName("QueuePreflight")
            .WithTags("Runs")
            .WithSummary("Queues a preflight-only run for a project.")
            .Produces<MigrationRunControlRecord>(StatusCodes.Status202Accepted)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status404NotFound);

        api.MapPost("/projects/{projectId}/runs", async (
                string projectId,
                CreateRunRequest request,
                AdminRunFactory factory,
                IAdminProjectStore store,
                RunPreflightGateService preflightGate,
                IMigrationRunQueue queue,
                [FromServices] ArtifactPathResolver artifactPathResolver,
                CancellationToken cancellationToken) =>
            {
                var project = await store.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
                if (project is null)
                {
                    return Results.NotFound(new AdminApiErrorResponse($"Project '{projectId}' was not found."));
                }

                CreateRunRequest resolvedRequest;
                try
                {
                    resolvedRequest = await artifactPathResolver
                        .ResolveRunRequestAsync(project, request, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
                {
                    return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                }

                var gate = await preflightGate
                    .ValidateRunCanStartAsync(project, resolvedRequest, cancellationToken)
                    .ConfigureAwait(false);

                if (!gate.IsAllowed)
                {
                    return Results.Conflict(new RunPreflightGateBlockedResponse(
                        gate.Message,
                        project.ProjectId,
                        RunPreflightRequiredAction));
                }

                var run = factory.CreateRun(project, resolvedRequest);

                await store.SaveRunAsync(run, cancellationToken).ConfigureAwait(false);
                await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false);

                return Results.Accepted($"/api/runs/{run.RunId}", run);
            })
            .WithName("QueueRun")
            .WithTags("Runs")
            .WithSummary("Queues a migration run for a project.")
            .Produces<MigrationRunControlRecord>(StatusCodes.Status202Accepted)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<RunPreflightGateBlockedResponse>(StatusCodes.Status409Conflict);

        api.MapGet("/runs", async (
                IAdminProjectStore store,
                CancellationToken cancellationToken) =>
                Results.Ok(await store.ListRunsAsync(cancellationToken).ConfigureAwait(false)))
            .WithName("GetRuns")
            .WithTags("Runs")
            .WithSummary("Lists migration runs.");

        api.MapGet("/runs/{runId}", async (
                string runId,
                IAdminProjectStore store,
                CancellationToken cancellationToken) =>
            {
                var run = await store.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
                return run is null ? Results.NotFound() : Results.Ok(run);
            })
            .WithName("GetRun")
            .WithTags("Runs")
            .WithSummary("Gets a migration run by id.")
            .Produces<MigrationRunControlRecord>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        api.MapGet("/runs/{runId}/lifecycle", async (
                string runId,
                IAdminProjectStore store,
                CancellationToken cancellationToken) =>
            {
                var run = await store.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
                return run is null
                    ? Results.NotFound()
                    : Results.Ok(RunLifecycleClassifier.Describe(run));
            })
            .WithName("GetRunLifecycle")
            .WithTags("Runs")
            .WithSummary("Gets normalized lifecycle semantics for a migration run.")
            .Produces<RunLifecycleDescriptor>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/runs/{runId}/cancel", async (
                string runId,
                IAdminProjectStore store,
                CancellationToken cancellationToken) =>
            {
                var run = await store.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
                if (run is null)
                {
                    return Results.NotFound();
                }

                if (!RunLifecycleClassifier.CanCancel(run.Status))
                {
                    return Results.Conflict(new RunStateConflictResponse($"Run is already terminal: {run.Status}."));
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
            .WithSummary("Marks a queued or running migration run as canceled in the control-plane store.")
            .Produces<MigrationRunControlRecord>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<RunStateConflictResponse>(StatusCodes.Status409Conflict);

        api.MapGet("/runs/{runId}/work-items", async (
                string runId,
                IAdminProjectStore store,
                IMigrationExecutionStateMaintenance state,
                CancellationToken cancellationToken) =>
            {
                var run = await store.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
                if (run is null)
                {
                    return Results.NotFound();
                }

                var items = await state.ListWorkItemsAsync(run.JobName, cancellationToken).ConfigureAwait(false);
                var matching = items
                    .Where(x =>
                        string.Equals(x.RunId, run.RunId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.JobName, run.JobName, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.UpdatedUtc)
                    .ToList();

                return Results.Ok(new RunWorkItemsResponse(run.RunId, run.JobName, matching.Count, matching));
            })
            .WithName("GetRunWorkItems")
            .WithTags("Runs")
            .WithSummary("Lists state-store work items associated with a migration run.")
            .Produces<RunWorkItemsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return api;
    }
}
