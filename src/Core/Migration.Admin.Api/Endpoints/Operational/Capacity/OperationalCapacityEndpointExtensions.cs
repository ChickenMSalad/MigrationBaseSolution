using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Migration.Admin.Api.Endpoints.Operational.Capacity;

public static class OperationalCapacityEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalCapacityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/capacity")
            .WithTags("Operational Capacity");

        group.MapGet("/summary", () =>
        {
            return Results.Ok(new
            {
                TotalWorkers = 0,
                ActiveWorkers = 0,
                QueueDepth = 0,
                EstimatedHoursRemaining = 0,
                Status = "not-wired"
            });
        });

        group.MapGet("/forecast", () =>
        {
            return Results.Ok(Array.Empty<object>());
        });

        return endpoints;
    }
}
