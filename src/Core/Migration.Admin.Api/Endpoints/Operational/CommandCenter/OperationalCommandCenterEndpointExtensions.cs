using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Migration.Admin.Api.Endpoints.Operational.CommandCenter;

public static class OperationalCommandCenterEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalCommandCenterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/command-center")
            .WithTags("Operational Command Center");

        group.MapGet("/summary", () =>
        {
            var response = new OperationalCommandCenterSummaryResponse(
                RuntimeStatus: "not-wired",
                ActiveRuns: 0,
                QueueDepth: 0,
                ActiveWorkers: 0,
                CriticalAlerts: 0,
                SlaSloBreaches: 0,
                EstimatedHoursRemaining: 0,
                EstimatedMonthlyCost: 0m,
                LastUpdatedUtc: DateTimeOffset.UtcNow);

            return Results.Ok(response);
        })
        .WithName("GetOperationalCommandCenterSummary");

        return endpoints;
    }
}

public sealed record OperationalCommandCenterSummaryResponse(
    string RuntimeStatus,
    int ActiveRuns,
    int QueueDepth,
    int ActiveWorkers,
    int CriticalAlerts,
    int SlaSloBreaches,
    decimal EstimatedHoursRemaining,
    decimal EstimatedMonthlyCost,
    DateTimeOffset LastUpdatedUtc);
