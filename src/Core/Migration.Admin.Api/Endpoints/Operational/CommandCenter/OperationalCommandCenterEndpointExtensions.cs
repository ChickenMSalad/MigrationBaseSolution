using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Endpoints.Operational.CommandCenter;

public static class OperationalCommandCenterEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalCommandCenterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/command-center")
            .WithTags("Operational Command Center");

        group.MapGet("/summary", async (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var connectionString =
                configuration.GetConnectionString("OperationalSql") ??
                configuration["OperationalSql:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return Results.Ok(CreateFallbackResponse("not-configured"));
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                var activeRuns = await ExecuteCountAsync(
                    connection,
                    "SELECT COUNT(1) FROM dbo.MigrationRuns;",
                    cancellationToken);

                var queueDepth = await ExecuteCountAsync(
                    connection,
                    "SELECT COUNT(1) FROM dbo.MigrationWorkItems;",
                    cancellationToken);

                var failures = await ExecuteCountAsync(
                    connection,
                    "SELECT COUNT(1) FROM dbo.MigrationFailures;",
                    cancellationToken);

                return Results.Ok(new OperationalCommandCenterSummaryResponse(
                    RuntimeStatus: "healthy",
                    ActiveRuns: activeRuns,
                    QueueDepth: queueDepth,
                    ActiveWorkers: 0,
                    CriticalAlerts: failures,
                    SlaSloBreaches: 0,
                    EstimatedHoursRemaining: 0,
                    EstimatedMonthlyCost: 0m,
                    LastUpdatedUtc: DateTimeOffset.UtcNow));
            }
            catch
            {
                return Results.Ok(CreateFallbackResponse("unhealthy"));
            }
        })
        .WithName("GetOperationalCommandCenterSummary");

        return endpoints;
    }

    private static async Task<int> ExecuteCountAsync(
        SqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static OperationalCommandCenterSummaryResponse CreateFallbackResponse(string status)
    {
        return new OperationalCommandCenterSummaryResponse(
            RuntimeStatus: status,
            ActiveRuns: 0,
            QueueDepth: 0,
            ActiveWorkers: 0,
            CriticalAlerts: 0,
            SlaSloBreaches: 0,
            EstimatedHoursRemaining: 0,
            EstimatedMonthlyCost: 0m,
            LastUpdatedUtc: DateTimeOffset.UtcNow);
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
