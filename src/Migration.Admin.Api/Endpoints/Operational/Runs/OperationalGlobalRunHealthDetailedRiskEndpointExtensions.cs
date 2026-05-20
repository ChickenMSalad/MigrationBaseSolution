using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalRunHealthDetailedRiskEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalRunHealthDetailedRiskEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/health-detailed-risk",
                async (
                    int? recentLimit,
                    int? metricsSampleLimit,
                    IOperationalGlobalRunHealthDetailedRiskService riskService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await riskService.GetDetailedRiskAsync(
                        recentLimit ?? 25,
                        metricsSampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalRunHealthDetailedRisk")
            .WithTags("Operational Store")
            .WithSummary("Gets detailed risk buckets for global operational run health.")
            .Produces<OperationalGlobalRunHealthDetailedRiskResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
