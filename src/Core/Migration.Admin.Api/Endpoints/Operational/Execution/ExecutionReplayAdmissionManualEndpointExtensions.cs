using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionReplayAdmissionManualEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayAdmissionManualEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-replay/admission")
            .WithTags("Operational Execution Replay");

        group.MapPost("/force-admit", async (
            IExecutionReplayAdmissionManualService service,
            ReplayAdmissionManualDecisionRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ForceAdmitAsync(request, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("ForceAdmitExecutionReplay");

        group.MapPost("/force-defer", async (
            IExecutionReplayAdmissionManualService service,
            ReplayAdmissionManualDecisionRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ForceDeferAsync(request, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("ForceDeferExecutionReplay");

        return endpoints;
    }
}

