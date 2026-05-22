using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Migration.Admin.Api.Endpoints.Operational.Cost;

public static class OperationalCostAnalyticsEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalCostAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/cost")
            .WithTags("Operational Cost Analytics");

        group.MapGet("/summary", () =>
        {
            return Results.Ok(new
            {
                EstimatedMonthlyCost = 0m,
                EstimatedQueueCost = 0m,
                EstimatedStorageCost = 0m,
                EstimatedComputeCost = 0m,
                Status = "not-wired"
            });
        });

        group.MapGet("/consumption", () =>
        {
            return Results.Ok(Array.Empty<object>());
        });

        return endpoints;
    }
}
