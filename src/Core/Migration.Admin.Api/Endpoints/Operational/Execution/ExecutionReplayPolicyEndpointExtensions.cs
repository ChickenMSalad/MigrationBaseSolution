using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionReplayPolicyEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayPolicyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-replay")
            .WithTags("Operational Execution Replay");

        group.MapGet("/{sourceExecutionSessionId:guid}/policy", async (
            IExecutionReplayPolicyService service,
            Guid sourceExecutionSessionId,
            string? scope,
            CancellationToken cancellationToken) =>
        {
            var result = await service.EvaluateAsync(
                sourceExecutionSessionId,
                scope ?? "failed-only",
                cancellationToken);

            return Results.Ok(result);
        })
        .WithName("EvaluateExecutionReplayPolicy");

        group.MapGet("/{sourceExecutionSessionId:guid}/policy/history", async (
            IExecutionReplayPolicyService service,
            Guid sourceExecutionSessionId,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var evaluations = await service.ReadHistoryAsync(
                sourceExecutionSessionId,
                Math.Clamp(take.GetValueOrDefault(25), 1, 250),
                cancellationToken);

            return Results.Ok(new ExecutionReplayPolicyEvaluationHistoryResponse(
                sourceExecutionSessionId,
                evaluations));
        })
        .WithName("GetExecutionReplayPolicyHistory");

        return endpoints;
    }
}


