using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionReplayApprovalEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayApprovalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-replay")
            .WithTags("Operational Execution Replay");

        group.MapPost("/approve", async (
            IExecutionReplayApprovalService service,
            ApproveExecutionReplayRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ApproveAsync(request, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("ApproveExecutionReplay");

        return endpoints;
    }
}
