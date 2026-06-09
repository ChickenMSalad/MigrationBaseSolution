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

        group.MapGet("/{sourceExecutionSessionId:guid}/approvals", async (
            IExecutionReplayApprovalService service,
            Guid sourceExecutionSessionId,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var approvals = await service.ReadHistoryAsync(
                sourceExecutionSessionId,
                Math.Clamp(take.GetValueOrDefault(25), 1, 250),
                cancellationToken);

            return Results.Ok(new ExecutionReplayApprovalHistoryResponse(
                sourceExecutionSessionId,
                approvals));
        })
        .WithName("GetExecutionReplayApprovalHistory");

        return endpoints;
    }
}

public sealed record ExecutionReplayApprovalHistoryResponse(
    Guid SourceExecutionSessionId,
    IReadOnlyList<ExecutionReplayApprovalRecord> Approvals);


