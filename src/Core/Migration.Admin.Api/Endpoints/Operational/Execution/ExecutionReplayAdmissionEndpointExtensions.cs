using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionReplayAdmissionEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayAdmissionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-replay")
            .WithTags("Operational Execution Replay");

        group.MapPost("/admission/evaluate", async (
            IExecutionReplayAdmissionService service,
            EvaluateExecutionReplayAdmissionRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await service.EvaluateAsync(request, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("EvaluateExecutionReplayAdmission");

        group.MapGet("/{executionSessionId:guid}/admission/history", async (
            IExecutionReplayAdmissionService service,
            Guid executionSessionId,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var decisions = await service.ReadHistoryAsync(
                executionSessionId,
                Math.Clamp(take.GetValueOrDefault(25), 1, 250),
                cancellationToken);

            return Results.Ok(new ExecutionReplayAdmissionDecisionHistoryResponse(
                executionSessionId,
                decisions));
        })
        .WithName("GetExecutionReplayAdmissionHistory");

        return endpoints;
    }
}
