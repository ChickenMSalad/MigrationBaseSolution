using Migration.Application.Operational.Runs;

namespace Migration.Admin.Api.Endpoints.Operational.SqlBackbone;

public static class SqlOperationalRunCoordinatorEndpointExtensions
{
    public static IEndpointRouteBuilder MapSqlOperationalRunCoordinatorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/sql-backbone/runs")
            .WithTags("SQL Operational Run Coordinator");

        group.MapGet("/{runId:guid}", GetRunAsync)
            .WithName("GetSqlOperationalRunCoordinatorRun");

        group.MapPost("/{runId:guid}/start", StartRunAsync)
            .WithName("StartSqlOperationalRun");

        group.MapPost("/{runId:guid}/cancel", RequestCancellationAsync)
            .WithName("CancelSqlOperationalRun");

        group.MapPost("/{runId:guid}/evaluate-completion", EvaluateCompletionAsync)
            .WithName("EvaluateSqlOperationalRunCompletion");

        return endpoints;
    }

    private static async Task<IResult> GetRunAsync(
        Guid runId,
        IOperationalRunCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        var run = await coordinator.GetRunAsync(runId, cancellationToken);
        return run is null ? Results.NotFound() : Results.Ok(run);
    }

    private static async Task<IResult> StartRunAsync(
        Guid runId,
        StartSqlOperationalRunRequest request,
        IOperationalRunCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        var result = await coordinator.StartRunAsync(new StartOperationalRunRequest(
            runId,
            request.CoordinatorId,
            request.BatchSize,
            request.WorkItemType,
            request.PartitionKey,
            request.Priority,
            request.PayloadTemplateJson), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> RequestCancellationAsync(
        Guid runId,
        CancelSqlOperationalRunRequest request,
        IOperationalRunCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        var result = await coordinator.RequestCancellationAsync(new RequestOperationalRunCancellation(
            runId,
            request.RequestedBy,
            request.Reason), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> EvaluateCompletionAsync(
        Guid runId,
        IOperationalRunCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        var result = await coordinator.EvaluateCompletionAsync(runId, cancellationToken);
        return Results.Ok(result);
    }

    public sealed record StartSqlOperationalRunRequest(
        string CoordinatorId,
        int BatchSize,
        string WorkItemType,
        string? PartitionKey,
        int Priority,
        string? PayloadTemplateJson);

    public sealed record CancelSqlOperationalRunRequest(
        string RequestedBy,
        string Reason);
}


