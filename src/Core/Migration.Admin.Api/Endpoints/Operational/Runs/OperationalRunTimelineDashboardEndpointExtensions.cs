using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunTimelineDashboardEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunTimelineDashboardEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/{runId:guid}/timeline/dashboard",
                async (
                    Guid runId,
                    int? previewLimit,
                    IOperationalRunTimelineDashboardService dashboardService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await dashboardService.GetDashboardAsync(
                            runId,
                            previewLimit ?? 10,
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
            .WithName("GetOperationalRunTimelineDashboard")
            .WithTags("Operational Store")
            .WithSummary("Gets a consolidated operational run timeline dashboard.")
            .Produces<OperationalRunTimelineDashboardResponse>(StatusCodes.Status200OK)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}


