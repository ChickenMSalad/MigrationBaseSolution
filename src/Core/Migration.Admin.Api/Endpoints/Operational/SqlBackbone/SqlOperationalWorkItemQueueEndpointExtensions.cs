using Microsoft.AspNetCore.Http.HttpResults;
using Migration.Application.Operational.WorkItems;

namespace Microsoft.AspNetCore.Builder;

public static class SqlOperationalWorkItemQueueEndpointExtensions
{
    public static IEndpointRouteBuilder MapSqlOperationalWorkItemQueueEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/sql-backbone/work-items")
            .WithTags("SQL Operational Work Items");

        group.MapPost("/enqueue", EnqueueAsync);
        group.MapPost("/claim", ClaimAsync);
        group.MapGet("/{workItemId:guid}", GetAsync);
        group.MapGet("/runs/{runId:guid}/summary", GetRunSummaryAsync);
        group.MapPost("/{workItemId:guid}/complete", CompleteAsync);
        group.MapPost("/{workItemId:guid}/fail", FailAsync);
        group.MapPost("/{workItemId:guid}/release", ReleaseAsync);

        return endpoints;
    }

    private static async Task<Ok<OperationalWorkItemRecord>> EnqueueAsync(
        EnqueueOperationalWorkItemRequest request,
        IOperationalWorkItemQueue queue,
        CancellationToken cancellationToken)
    {
        var result = await queue.EnqueueAsync(request, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<IReadOnlyList<OperationalWorkItemRecord>>> ClaimAsync(
        ClaimOperationalWorkItemsRequest request,
        IOperationalWorkItemQueue queue,
        CancellationToken cancellationToken)
    {
        var result = await queue.ClaimAsync(request, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<OperationalWorkItemRecord>, NotFound>> GetAsync(
        long workItemId,
        IOperationalWorkItemQueue queue,
        CancellationToken cancellationToken)
    {
        var result = await queue.GetAsync(workItemId, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Ok<OperationalWorkItemRunSummary>> GetRunSummaryAsync(
        Guid runId,
        IOperationalWorkItemQueue queue,
        CancellationToken cancellationToken)
    {
        var result = await queue.GetRunSummaryAsync(runId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> CompleteAsync(
        long workItemId,
        CompleteSqlOperationalWorkItemRequest request,
        IOperationalWorkItemQueue queue,
        CancellationToken cancellationToken)
    {
        await queue.CompleteAsync(new CompleteOperationalWorkItemRequest(workItemId, request.WorkerId, request.ResultJson), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> FailAsync(
        long workItemId,
        FailSqlOperationalWorkItemRequest request,
        IOperationalWorkItemQueue queue,
        CancellationToken cancellationToken)
    {
        await queue.FailAsync(new FailOperationalWorkItemRequest(
            workItemId,
            request.WorkerId,
            request.ErrorCode,
            request.ErrorMessage,
            request.IsRetryable,
            request.NextAttemptUtc), cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ReleaseAsync(
        long workItemId,
        ReleaseSqlOperationalWorkItemRequest request,
        IOperationalWorkItemQueue queue,
        CancellationToken cancellationToken)
    {
        await queue.ReleaseAsync(new ReleaseOperationalWorkItemRequest(workItemId, request.WorkerId, request.NextAttemptUtc), cancellationToken);
        return TypedResults.NoContent();
    }

    public sealed record CompleteSqlOperationalWorkItemRequest(string WorkerId, string? ResultJson);

    public sealed record FailSqlOperationalWorkItemRequest(
        string WorkerId,
        string ErrorCode,
        string ErrorMessage,
        bool IsRetryable,
        DateTimeOffset? NextAttemptUtc);

    public sealed record ReleaseSqlOperationalWorkItemRequest(string WorkerId, DateTimeOffset? NextAttemptUtc);
}
