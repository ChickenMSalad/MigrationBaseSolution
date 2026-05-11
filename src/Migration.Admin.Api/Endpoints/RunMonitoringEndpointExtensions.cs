using Migration.ControlPlane.Models;
using Migration.ControlPlane.Services;

namespace Migration.Admin.Api.Endpoints;

public static class RunMonitoringEndpointExtensions
{
    public static RouteGroupBuilder MapRunMonitoringEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/runs/{runId}/summary", async (string runId, RunMonitoringService monitoring, CancellationToken cancellationToken) =>
        {
            var summary = await monitoring.GetSummaryAsync(runId, cancellationToken).ConfigureAwait(false);
            return summary is null ? Results.NotFound() : Results.Ok(summary);
        })
        .WithName("GetRunSummary")
        .WithTags("Runs")
        .WithSummary("Gets aggregate status counts, progress, and recent failures for a migration run.");

        api.MapGet("/runs/{runId}/failures", async (string runId, RunMonitoringService monitoring, CancellationToken cancellationToken) =>
        {
            var failures = await monitoring.GetFailuresAsync(runId, cancellationToken).ConfigureAwait(false);
            return failures is null ? Results.NotFound() : Results.Ok(new RunFailuresResponse(runId, failures.Count, failures));
        })
        .WithName("GetRunFailures")
        .WithTags("Runs")
        .WithSummary("Gets failed/validation-failed work items for a migration run.");

        api.MapGet("/runs/{runId}/events", async (string runId, int? take, RunMonitoringService monitoring, CancellationToken cancellationToken) =>
        {
            var events = await monitoring.GetEventsAsync(runId, take ?? 500, cancellationToken).ConfigureAwait(false);
            return events is null ? Results.NotFound() : Results.Ok(new RunEventsResponse(runId, events.Count, events));
        })
        .WithName("GetRunEvents")
        .WithTags("Runs")
        .WithSummary("Gets persisted progress events for a migration run.");

        return api;
    }
}
