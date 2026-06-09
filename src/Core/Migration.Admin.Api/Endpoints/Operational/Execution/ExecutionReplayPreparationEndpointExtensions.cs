using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionReplayPreparationEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayPreparationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-replay")
            .WithTags("Operational Execution Replay");

        group.MapPost("/prepare", async (
            IExecutionReplayPreparationService service,
            PrepareExecutionReplayRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await service.PrepareAsync(request, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("PrepareExecutionReplayManifest");

        return endpoints;
    }
}


