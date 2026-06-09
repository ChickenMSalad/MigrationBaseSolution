using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionReplayPolicyOverrideEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayPolicyOverrideEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-replay")
            .WithTags("Operational Execution Replay");

        group.MapPost("/policy/override", async (
            IExecutionReplayPolicyOverrideService service,
            OverrideExecutionReplayPolicyRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await service.OverrideAsync(request, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("OverrideExecutionReplayPolicy");

        group.MapGet("/{sourceExecutionSessionId:guid}/policy/overrides", async (
            IExecutionReplayPolicyOverrideService service,
            Guid sourceExecutionSessionId,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var records = await service.ReadHistoryAsync(
                sourceExecutionSessionId,
                Math.Clamp(take.GetValueOrDefault(25), 1, 250),
                cancellationToken);

            return Results.Ok(new ExecutionReplayPolicyOverrideHistoryResponse(
                sourceExecutionSessionId,
                records));
        })
        .WithName("GetExecutionReplayPolicyOverrideHistory");

        return endpoints;
    }
}


