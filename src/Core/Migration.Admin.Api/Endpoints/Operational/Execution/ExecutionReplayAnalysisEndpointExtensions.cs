using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionReplayAnalysisEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayAnalysisEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-replay")
            .WithTags("Operational Execution Replay");

        group.MapGet("/{executionSessionId:guid}/analysis", async (
            IExecutionReplayAnalysisService service,
            Guid executionSessionId,
            CancellationToken cancellationToken) =>
        {
            var result = await service.AnalyzeAsync(executionSessionId, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("AnalyzeExecutionReplayReadiness");

        return endpoints;
    }
}
