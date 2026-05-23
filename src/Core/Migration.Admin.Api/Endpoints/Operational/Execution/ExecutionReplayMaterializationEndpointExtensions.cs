using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionReplayMaterializationEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayMaterializationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/operational/execution-replay").WithTags("Operational Execution Replay");

        group.MapPost("/materialize", async (
            IExecutionReplayMaterializationService service,
            MaterializeExecutionReplayRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await service.MaterializeAsync(request, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("MaterializeExecutionReplay");

        return endpoints;
    }
}
