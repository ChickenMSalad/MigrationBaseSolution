using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionReplayLineageEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayLineageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-replay")
            .WithTags("Operational Execution Replay");

        group.MapGet("/{executionSessionId:guid}/lineage", async (
            IExecutionReplayLineageService service,
            Guid executionSessionId,
            CancellationToken cancellationToken) =>
        {
            var lineage = await service.ReadLineageAsync(executionSessionId, cancellationToken);
            return Results.Ok(lineage);
        })
        .WithName("GetExecutionReplayLineage");

        return endpoints;
    }
}
