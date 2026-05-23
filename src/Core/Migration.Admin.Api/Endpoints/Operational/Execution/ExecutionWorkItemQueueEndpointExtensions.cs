using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionWorkItemQueueEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionWorkItemQueueEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-work-items")
            .WithTags("Operational Execution Work Items");

        group.MapPost("/expand", async (
            IExecutionWorkItemQueueStore store,
            ExpandExecutionPlanToWorkItemsRequest request,
            CancellationToken cancellationToken) =>
        {
            var items = await store.ExpandFromPlanAsync(request, cancellationToken);
            return Results.Ok(new ExecutionWorkItemListResponse(request.ExecutionSessionId, items));
        })
        .WithName("ExpandExecutionPlanToWorkItems");

        group.MapPost("/lease", async (
            IExecutionWorkItemQueueStore store,
            LeaseExecutionWorkItemsRequest request,
            CancellationToken cancellationToken) =>
        {
            var items = await store.LeaseAsync(request, cancellationToken);
            return Results.Ok(new ExecutionWorkItemListResponse(request.ExecutionSessionId, items));
        })
        .WithName("LeaseExecutionWorkItems");

        group.MapPost("/renew", async (
            IExecutionWorkItemQueueStore store,
            RenewExecutionWorkItemLeaseRequest request,
            CancellationToken cancellationToken) =>
        {
            await store.RenewLeaseAsync(request, cancellationToken);
            return Results.Ok();
        })
        .WithName("RenewExecutionWorkItemLease");

        group.MapPost("/requeue", async (
            IExecutionWorkItemQueueStore store,
            RequeueExecutionWorkItemsRequest request,
            CancellationToken cancellationToken) =>
        {
            var count = await store.RequeueAsync(request, cancellationToken);
            return Results.Ok(new ExecutionWorkItemRequeueResponse(request.ExecutionSessionId, count));
        })
        .WithName("RequeueExecutionWorkItems");

        group.MapPost("/complete", async (
            IExecutionWorkItemQueueStore store,
            CompleteExecutionWorkItemRequest request,
            CancellationToken cancellationToken) =>
        {
            await store.CompleteAsync(request, cancellationToken);
            return Results.Ok();
        })
        .WithName("CompleteExecutionWorkItem");

        group.MapPost("/fail", async (
            IExecutionWorkItemQueueStore store,
            FailExecutionWorkItemRequest request,
            CancellationToken cancellationToken) =>
        {
            await store.FailAsync(request, cancellationToken);
            return Results.Ok();
        })
        .WithName("FailExecutionWorkItem");

        group.MapGet("/{executionSessionId:guid}/summary", async (
            IExecutionWorkItemQueueStore store,
            Guid executionSessionId,
            CancellationToken cancellationToken) =>
        {
            var summary = await store.ReadSummaryAsync(executionSessionId, cancellationToken);
            return Results.Ok(summary);
        })
        .WithName("GetExecutionWorkItemQueueSummary");

        group.MapGet("/{executionSessionId:guid}/recent", async (
            IExecutionWorkItemQueueStore store,
            Guid executionSessionId,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var safeTake = Math.Clamp(take.GetValueOrDefault(100), 1, 1000);
            var items = await store.ReadRecentAsync(executionSessionId, safeTake, cancellationToken);
            return Results.Ok(new ExecutionWorkItemListResponse(executionSessionId, items));
        })
        .WithName("GetRecentExecutionWorkItems");

        return endpoints;
    }
}

public sealed record ExecutionWorkItemListResponse(
    Guid ExecutionSessionId,
    IReadOnlyList<ExecutionWorkItemRecord> Items);

public sealed record ExecutionWorkItemRequeueResponse(
    Guid ExecutionSessionId,
    int Requeued);
