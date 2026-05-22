using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionLifecycleEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionLifecycleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-lifecycle")
            .WithTags("Operational Execution Lifecycle");

        group.MapGet("/phases", () =>
        {
            return Results.Ok(new ExecutionPhaseCatalogResponse(ExecutionPhaseNames.All));
        })
        .WithName("GetExecutionPhaseCatalog");

        group.MapPost("/transition", async (
            IExecutionLifecycleService lifecycleService,
            TransitionExecutionPhaseRequest request,
            CancellationToken cancellationToken) =>
        {
            var record = await lifecycleService.TransitionAsync(request, cancellationToken);
            return Results.Ok(record);
        })
        .WithName("TransitionExecutionPhase");

        group.MapGet("/{executionSessionId:guid}/history", async (
            IExecutionLifecycleService lifecycleService,
            Guid executionSessionId,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var safeTake = Math.Clamp(take.GetValueOrDefault(25), 1, 250);
            var history = await lifecycleService.ReadRecentHistoryAsync(
                executionSessionId,
                safeTake,
                cancellationToken);

            return Results.Ok(new ExecutionPhaseHistoryResponse(
                ExecutionSessionId: executionSessionId,
                Take: safeTake,
                History: history));
        })
        .WithName("GetExecutionPhaseHistory");

        return endpoints;
    }
}

public sealed record ExecutionPhaseCatalogResponse(
    IReadOnlyList<string> Phases);

public sealed record ExecutionPhaseHistoryResponse(
    Guid ExecutionSessionId,
    int Take,
    IReadOnlyList<ExecutionPhaseHistoryRecord> History);
