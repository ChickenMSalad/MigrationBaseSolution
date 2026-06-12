using Migration.Application.Operational.Readiness;

namespace Migration.Admin.Api.Endpoints.Operational.SqlBackbone;

public static class SqlOperationalRuntimeReadinessEndpointExtensions
{
    public static IEndpointRouteBuilder MapSqlOperationalRuntimeReadinessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/sql-backbone/runtime")
            .WithTags("SQL Operational Runtime Readiness");

        group.MapGet("/readiness", GetReadinessAsync)
            .WithName("GetSqlOperationalRuntimeReadiness");

        group.MapGet("/runs/{runId:guid}/readiness", GetRunReadinessAsync)
            .WithName("GetSqlOperationalRunReadiness");

        return endpoints;
    }

    private static async Task<IResult> GetReadinessAsync(
        IOperationalRuntimeReadinessService readinessService,
        CancellationToken cancellationToken)
    {
        var report = await readinessService.GetReadinessAsync(cancellationToken);
        return report.IsReady ? Results.Ok(report) : Results.Problem(
            title: "SQL operational runtime is not ready.",
            detail: string.Join(" ", report.BlockingIssues),
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?>
            {
                ["readiness"] = report
            });
    }

    private static async Task<IResult> GetRunReadinessAsync(
        Guid runId,
        IOperationalRuntimeReadinessService readinessService,
        CancellationToken cancellationToken)
    {
        var report = await readinessService.GetRunReadinessAsync(runId, cancellationToken);
        return report.Status == "NotFound" ? Results.NotFound(report) : Results.Ok(report);
    }
}


