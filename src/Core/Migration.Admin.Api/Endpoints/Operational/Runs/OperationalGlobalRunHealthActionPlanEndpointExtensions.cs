using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalRunHealthActionPlanEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalRunHealthActionPlanEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/health-action-plan",
                async (
                    int? recentLimit,
                    int? metricsSampleLimit,
                    IOperationalGlobalRunHealthActionPlanService actionPlanService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await actionPlanService.GetActionPlanAsync(
                        recentLimit ?? 25,
                        metricsSampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalRunHealthActionPlan")
            .WithTags("Operational Store")
            .WithSummary("Gets an ordered operational action plan for global run health.")
            .Produces<OperationalGlobalRunHealthActionPlanResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


