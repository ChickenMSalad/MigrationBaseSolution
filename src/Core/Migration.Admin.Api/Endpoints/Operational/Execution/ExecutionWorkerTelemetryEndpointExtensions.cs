using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionWorkerTelemetryEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionWorkerTelemetryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-workers")
            .WithTags("Operational Execution Workers");

        group.MapPost("/heartbeat", async (
            IExecutionWorkerHeartbeatStore store,
            ExecutionWorkerHeartbeatRequest request,
            CancellationToken cancellationToken) =>
        {
            await store.UpsertAsync(request, cancellationToken);
            return Results.Ok();
        })
        .WithName("RecordExecutionWorkerHeartbeat");

        group.MapGet("/summary", async (
            IExecutionWorkerHeartbeatStore store,
            int? staleAfterSeconds,
            CancellationToken cancellationToken) =>
        {
            var summary = await store.ReadSummaryAsync(
                staleAfterSeconds.GetValueOrDefault(120),
                cancellationToken);

            return Results.Ok(summary);
        })
        .WithName("GetExecutionWorkerTelemetrySummary");

        return endpoints;
    }
}


