using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Endpoints.Operational.Cost;

public static class OperationalCostAnalyticsEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalCostAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/cost")
            .WithTags("Operational Cost Analytics");

        group.MapGet("/summary", async (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var snapshot = await CostAnalyticsReader.ReadAsync(configuration, cancellationToken);
            var response = new OperationalCostSummaryResponse(
                EstimatedMonthlyCost: snapshot.EstimatedMonthlyCost,
                EstimatedQueueCost: snapshot.EstimatedQueueCost,
                EstimatedStorageCost: snapshot.EstimatedStorageCost,
                EstimatedComputeCost: snapshot.EstimatedComputeCost,
                TotalWorkItems: snapshot.TotalWorkItems,
                PendingWorkItems: snapshot.PendingWorkItems,
                RunningWorkItems: snapshot.RunningWorkItems,
                CompletedWorkItems: snapshot.CompletedWorkItems,
                FailedWorkItems: snapshot.FailedWorkItems,
                ManifestRows: snapshot.ManifestRows,
                ActiveRuns: snapshot.ActiveRuns,
                CompletedRuns: snapshot.CompletedRuns,
                Status: snapshot.Status,
                Message: snapshot.Message,
                GeneratedUtc: DateTimeOffset.UtcNow);

            return Results.Ok(response);
        })
        .WithName("GetOperationalCostSummary");

        group.MapGet("/consumption", async (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var snapshot = await CostAnalyticsReader.ReadAsync(configuration, cancellationToken);
            var items = new List<OperationalCostConsumptionItemResponse>
            {
                new("work-items-total", "Work items", snapshot.TotalWorkItems, "items", snapshot.EstimatedQueueCost, "Operational work items in migration.WorkItems."),
                new("manifest-rows", "Manifest rows", snapshot.ManifestRows, "rows", snapshot.EstimatedStorageCost, "Operational manifest rows in migration.ManifestRows."),
                new("running-work-items", "Running work", snapshot.RunningWorkItems, "items", snapshot.EstimatedComputeCost, "Currently running work items."),
                new("failed-work-items", "Failed work", snapshot.FailedWorkItems, "items", 0m, "Failed work items that may require retry."),
                new("active-runs", "Active runs", snapshot.ActiveRuns, "runs", 0m, "Runs not yet completed in migration.Runs.")
            };

            return Results.Ok(new OperationalCostConsumptionResponse(DateTimeOffset.UtcNow, snapshot.Status, items));
        })
        .WithName("GetOperationalCostConsumption");

        return endpoints;
    }

    private static class CostAnalyticsReader
    {
        public static async Task<CostAnalyticsSnapshot> ReadAsync(IConfiguration configuration, CancellationToken cancellationToken)
        {
            var connectionString = configuration.GetConnectionString("OperationalSql")
                ?? configuration.GetConnectionString("MigrationOperationalStore")
                ?? configuration["OperationalSql:ConnectionString"]
                ?? configuration["MigrationOperationalStore:ConnectionString"]
                ?? configuration["ConnectionStrings:OperationalSql"]
                ?? configuration["ConnectionStrings:MigrationOperationalStore"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return CostAnalyticsSnapshot.NotConfigured("Operational SQL connection string is not configured.");
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                var hasRuns = await TableExistsAsync(connection, "migration", "Runs", cancellationToken);
                var hasWorkItems = await TableExistsAsync(connection, "migration", "WorkItems", cancellationToken);
                var hasManifestRows = await TableExistsAsync(connection, "migration", "ManifestRows", cancellationToken);

                if (!hasRuns || !hasWorkItems || !hasManifestRows)
                {
                    var missing = new List<string>();
                    if (!hasRuns) { missing.Add("migration.Runs"); }
                    if (!hasWorkItems) { missing.Add("migration.WorkItems"); }
                    if (!hasManifestRows) { missing.Add("migration.ManifestRows"); }

                    return CostAnalyticsSnapshot.Unhealthy("Missing SQL runtime table(s): " + string.Join(", ", missing));
                }

                var totalWorkItems = await CountAsync(connection, "SELECT COUNT(1) FROM migration.WorkItems;", cancellationToken);
                var pendingWorkItems = await CountAsync(connection, "SELECT COUNT(1) FROM migration.WorkItems WHERE Status IN ('Queued', 'Pending', 'Ready', 'Dispatched');", cancellationToken);
                var runningWorkItems = await CountAsync(connection, "SELECT COUNT(1) FROM migration.WorkItems WHERE Status IN ('Running', 'InProgress', 'Processing');", cancellationToken);
                var completedWorkItems = await CountAsync(connection, "SELECT COUNT(1) FROM migration.WorkItems WHERE Status IN ('Completed', 'Succeeded', 'Success');", cancellationToken);
                var failedWorkItems = await CountAsync(connection, "SELECT COUNT(1) FROM migration.WorkItems WHERE Status IN ('Failed', 'Error', 'DeadLettered');", cancellationToken);
                var manifestRows = await CountAsync(connection, "SELECT COUNT(1) FROM migration.ManifestRows;", cancellationToken);
                var activeRuns = await CountAsync(connection, "SELECT COUNT(1) FROM migration.Runs WHERE Status NOT IN ('Completed', 'CompletedWithFailures', 'Failed', 'Canceled', 'Cancelled');", cancellationToken);
                var completedRuns = await CountAsync(connection, "SELECT COUNT(1) FROM migration.Runs WHERE Status IN ('Completed', 'CompletedWithFailures');", cancellationToken);

                var estimatedQueueCost = Decimal.Round(totalWorkItems * 0.00002m, 4);
                var estimatedStorageCost = Decimal.Round(manifestRows * 0.000005m, 4);
                var estimatedComputeCost = Decimal.Round((runningWorkItems + activeRuns) * 0.01m, 4);
                var estimatedMonthlyCost = Decimal.Round(estimatedQueueCost + estimatedStorageCost + estimatedComputeCost, 4);

                return new CostAnalyticsSnapshot(
                    Status: "healthy",
                    Message: null,
                    EstimatedMonthlyCost: estimatedMonthlyCost,
                    EstimatedQueueCost: estimatedQueueCost,
                    EstimatedStorageCost: estimatedStorageCost,
                    EstimatedComputeCost: estimatedComputeCost,
                    TotalWorkItems: totalWorkItems,
                    PendingWorkItems: pendingWorkItems,
                    RunningWorkItems: runningWorkItems,
                    CompletedWorkItems: completedWorkItems,
                    FailedWorkItems: failedWorkItems,
                    ManifestRows: manifestRows,
                    ActiveRuns: activeRuns,
                    CompletedRuns: completedRuns);
            }
            catch (Exception ex)
            {
                return CostAnalyticsSnapshot.Unhealthy(ex.Message);
            }
        }

        private static async Task<bool> TableExistsAsync(SqlConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CASE WHEN OBJECT_ID(@name, 'U') IS NULL THEN 0 ELSE 1 END;";
            command.Parameters.AddWithValue("@name", schemaName + "." + tableName);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) == 1;
        }

        private static async Task<int> CountAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
    }
}

public sealed record OperationalCostSummaryResponse(
    decimal EstimatedMonthlyCost,
    decimal EstimatedQueueCost,
    decimal EstimatedStorageCost,
    decimal EstimatedComputeCost,
    int TotalWorkItems,
    int PendingWorkItems,
    int RunningWorkItems,
    int CompletedWorkItems,
    int FailedWorkItems,
    int ManifestRows,
    int ActiveRuns,
    int CompletedRuns,
    string Status,
    string? Message,
    DateTimeOffset GeneratedUtc);

public sealed record OperationalCostConsumptionResponse(
    DateTimeOffset GeneratedUtc,
    string Status,
    IReadOnlyList<OperationalCostConsumptionItemResponse> Items);

public sealed record OperationalCostConsumptionItemResponse(
    string MetricId,
    string Name,
    int Quantity,
    string Unit,
    decimal EstimatedCost,
    string Description);

internal sealed record CostAnalyticsSnapshot(
    string Status,
    string? Message,
    decimal EstimatedMonthlyCost,
    decimal EstimatedQueueCost,
    decimal EstimatedStorageCost,
    decimal EstimatedComputeCost,
    int TotalWorkItems,
    int PendingWorkItems,
    int RunningWorkItems,
    int CompletedWorkItems,
    int FailedWorkItems,
    int ManifestRows,
    int ActiveRuns,
    int CompletedRuns)
{
    public static CostAnalyticsSnapshot NotConfigured(string message)
    {
        return Empty("not-configured", message);
    }

    public static CostAnalyticsSnapshot Unhealthy(string message)
    {
        return Empty("unhealthy", message);
    }

    private static CostAnalyticsSnapshot Empty(string status, string message)
    {
        return new CostAnalyticsSnapshot(
            Status: status,
            Message: message,
            EstimatedMonthlyCost: 0m,
            EstimatedQueueCost: 0m,
            EstimatedStorageCost: 0m,
            EstimatedComputeCost: 0m,
            TotalWorkItems: 0,
            PendingWorkItems: 0,
            RunningWorkItems: 0,
            CompletedWorkItems: 0,
            FailedWorkItems: 0,
            ManifestRows: 0,
            ActiveRuns: 0,
            CompletedRuns: 0);
    }
}
