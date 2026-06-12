using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Operational.SqlMetrics;

public sealed class SqlOperationalMetricsReader : ISqlOperationalMetricsReader
{
    private readonly IConfiguration _configuration;

    public SqlOperationalMetricsReader(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<SqlOperationalMetricsSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken)
    {
        var connectionString =
            _configuration.GetConnectionString("OperationalSql") ??
            _configuration["OperationalSql:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return CreateFallback("not-configured", "Operational SQL connection string is not configured.");
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var activeRuns = await ExecuteCountAsync(
                connection,
                "SELECT COUNT(1) FROM migration.Runs;",
                cancellationToken);

            var queueDepth = await ExecuteCountAsync(
                connection,
                "SELECT COUNT(1) FROM migration.WorkItems;",
                cancellationToken);

            var failureCount = await ExecuteCountAsync(
                connection,
                "SELECT COUNT(1) FROM migration.WorkItemFailures;",
                cancellationToken);

            return new SqlOperationalMetricsSnapshot(
                Status: "healthy",
                ActiveRuns: activeRuns,
                QueueDepth: queueDepth,
                FailureCount: failureCount,
                ActiveWorkers: 0,
                SlaSloBreaches: 0,
                EstimatedHoursRemaining: 0,
                EstimatedMonthlyCost: 0m,
                Message: null);
        }
        catch (Exception ex)
        {
            return CreateFallback("unhealthy", ex.Message);
        }
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

    private static SqlOperationalMetricsSnapshot CreateFallback(string status, string message)
    {
        return new SqlOperationalMetricsSnapshot(
            Status: status,
            ActiveRuns: 0,
            QueueDepth: 0,
            FailureCount: 0,
            ActiveWorkers: 0,
            SlaSloBreaches: 0,
            EstimatedHoursRemaining: 0,
            EstimatedMonthlyCost: 0m,
            Message: message);
    }
}


