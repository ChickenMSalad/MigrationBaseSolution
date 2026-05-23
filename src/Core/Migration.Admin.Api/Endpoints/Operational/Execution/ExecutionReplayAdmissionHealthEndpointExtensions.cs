using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionReplayAdmissionHealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayAdmissionHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-replay/admission")
            .WithTags("Operational Execution Replay");

        group.MapPost("/health/evaluate", async (
            IExecutionReplayAdmissionHealthService service,
            EvaluateExecutionReplayAdmissionHealthRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await service.EvaluateAsync(request, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("EvaluateExecutionReplayAdmissionHealth");

        return endpoints;
    }
}
