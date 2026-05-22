using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionPlanEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionPlanEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-plan")
            .WithTags("Operational Execution Plan");

        group.MapPost("/seed", async (
            IExecutionPlanStore store,
            SeedExecutionPlanRequest request,
            CancellationToken cancellationToken) =>
        {
            var steps = await store.SeedDefaultPlanAsync(request, cancellationToken);
            return Results.Ok(new ExecutionPlanResponse(request.ExecutionSessionId, steps));
        })
        .WithName("SeedExecutionPlan");

        group.MapGet("/{executionSessionId:guid}", async (
            IExecutionPlanStore store,
            Guid executionSessionId,
            CancellationToken cancellationToken) =>
        {
            var steps = await store.ReadPlanAsync(executionSessionId, cancellationToken);
            return Results.Ok(new ExecutionPlanResponse(executionSessionId, steps));
        })
        .WithName("GetExecutionPlan");

        return endpoints;
    }
}

public sealed record ExecutionPlanResponse(
    Guid ExecutionSessionId,
    IReadOnlyList<ExecutionPlanStepRecord> Steps);
