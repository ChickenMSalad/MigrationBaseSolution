using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunDashboardEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunDashboardEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/{runId:guid}/dashboard",
                async (
                    Guid runId,
                    IOperationalRunDashboardSummaryService dashboardService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await dashboardService.GetSummaryAsync(
                            runId,
                            cancellationToken);

                        return response is null
                            ? Results.NotFound(new AdminApiErrorResponse("Operational run was not found."))
                            : Results.Ok(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("GetOperationalRunDashboard")
            .WithTags("Operational Store")
            .WithSummary("Gets a consolidated dashboard summary for one operational run.")
            .Produces<OperationalRunDashboardSummaryResponse>(StatusCodes.Status200OK)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}
