using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionSessionEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-sessions")
            .WithTags("Operational Execution Sessions");

        group.MapPost("/", async (
            IExecutionSessionStore store,
            CreateExecutionSessionRequest request,
            CancellationToken cancellationToken) =>
        {
            var session = await store.CreateAsync(request, cancellationToken);
            return Results.Ok(session);
        })
        .WithName("CreateExecutionSession");

        group.MapGet("/recent", async (
            IExecutionSessionStore store,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var safeTake = Math.Clamp(take.GetValueOrDefault(50), 1, 250);
            var sessions = await store.ReadRecentAsync(safeTake, cancellationToken);

            return Results.Ok(new RecentExecutionSessionsResponse(
                Take: safeTake,
                Sessions: sessions));
        })
        .WithName("GetRecentExecutionSessions");

        return endpoints;
    }
}

public sealed record RecentExecutionSessionsResponse(
    int Take,
    IReadOnlyList<ExecutionSessionRecord> Sessions);


