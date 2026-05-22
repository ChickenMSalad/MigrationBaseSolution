using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.SqlMetrics;

namespace Migration.Admin.Api.Endpoints.Operational.CommandCenter;

public static class OperationalCommandCenterEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalCommandCenterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/command-center")
            .WithTags("Operational Command Center");

        group.MapGet("/summary", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);

            var response = new OperationalCommandCenterSummaryResponse(
                RuntimeStatus: snapshot.Status,
                ActiveRuns: snapshot.ActiveRuns,
                QueueDepth: snapshot.QueueDepth,
                ActiveWorkers: snapshot.ActiveWorkers,
                CriticalAlerts: snapshot.FailureCount,
                SlaSloBreaches: snapshot.SlaSloBreaches,
                EstimatedHoursRemaining: snapshot.EstimatedHoursRemaining,
                EstimatedMonthlyCost: snapshot.EstimatedMonthlyCost,
                LastUpdatedUtc: DateTimeOffset.UtcNow,
                Message: snapshot.Message);

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
    DateTimeOffset LastUpdatedUtc,
    string? Message);
