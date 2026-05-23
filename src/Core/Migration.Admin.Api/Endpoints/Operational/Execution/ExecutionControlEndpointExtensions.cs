using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionControlEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionControlEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-control")
            .WithTags("Operational Execution Control");

        group.MapPost("/pause", async (
            IExecutionControlService service,
            PauseExecutionSessionRequest request,
            CancellationToken cancellationToken) =>
        {
            await service.PauseAsync(request, cancellationToken);
            return Results.Ok();
        })
        .WithName("PauseExecutionSession");

        group.MapPost("/resume", async (
            IExecutionControlService service,
            ResumeExecutionSessionRequest request,
            CancellationToken cancellationToken) =>
        {
            await service.ResumeAsync(request, cancellationToken);
            return Results.Ok();
        })
        .WithName("ResumeExecutionSession");

        return endpoints;
    }
}
