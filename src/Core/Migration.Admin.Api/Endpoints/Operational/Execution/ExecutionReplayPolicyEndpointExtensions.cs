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

        return endpoints;
    }
}
