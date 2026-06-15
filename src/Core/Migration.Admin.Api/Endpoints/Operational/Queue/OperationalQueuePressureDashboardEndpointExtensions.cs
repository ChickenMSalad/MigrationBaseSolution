using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// SQL-backed queue pressure dashboard endpoints.
/// This endpoint reads the operational runtime tables directly and does not use file state or Admin JSON.
/// </summary>
public static class OperationalQueuePressureDashboardEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureDashboardApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
            "/operational/queue-pressure/dashboard",
            async (IConfiguration configuration, CancellationToken cancellationToken) =>
            {
                var snapshot = await SqlQueuePressureSnapshotReader.ReadAsync(configuration, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    dashboard = new
                    {
                        generatedAtUtc = snapshot.GeneratedAtUtc,
                        pressureSignals = new
                        {
                            queueDepth = snapshot.QueueDepth,
                            runningDepth = snapshot.RunningDepth,
                            retryDepth = snapshot.RetryDepth,
                            failedDepth = snapshot.FailedDepth,
                            completedDepth = snapshot.CompletedDepth,
                            staleRunningDepth = snapshot.StaleRunningDepth,
                            oldestQueuedUtc = snapshot.OldestQueuedUtc,
                            oldestRunningUtc = snapshot.OldestRunningUtc,
                            newestCompletedUtc = snapshot.NewestCompletedUtc,
                            statusCounts = snapshot.StatusCounts,
                            recentRuns = snapshot.RecentRuns
                        },
                        readiness = new
                        {
                            snapshot.Available,
                            snapshot.Source,
                            snapshot.Error,
                            isBacklogPresent = snapshot.QueueDepth > 0,
                            isRunningPresent = snapshot.RunningDepth > 0,
                            isRetryPressurePresent = snapshot.RetryDepth > 0,
                            isFailurePressurePresent = snapshot.FailedDepth > 0,
                            isStaleRunningPressurePresent = snapshot.StaleRunningDepth > 0
                        }
                    }
                });
            })
            .WithName("GetOperationalQueuePressureDashboard")
            .WithTags("Operational Store")
            .WithSummary("Gets a SQL-backed operational queue pressure dashboard.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
            "/operational/queue-pressure/summary",
            async (IConfiguration configuration, CancellationToken cancellationToken) =>
            {
                var snapshot = await SqlQueuePressureSnapshotReader.ReadAsync(configuration, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    summary = new
                    {
                        generatedAtUtc = snapshot.GeneratedAtUtc,
                        snapshot.Available,
                        snapshot.Source,
                        snapshot.Error,
                        queueDepth = snapshot.QueueDepth,
                        runningDepth = snapshot.RunningDepth,
                        retryDepth = snapshot.RetryDepth,
                        failedDepth = snapshot.FailedDepth,
                        completedDepth = snapshot.CompletedDepth,
                        staleRunningDepth = snapshot.StaleRunningDepth,
                        oldestQueuedUtc = snapshot.OldestQueuedUtc,
                        oldestRunningUtc = snapshot.OldestRunningUtc,
                        newestCompletedUtc = snapshot.NewestCompletedUtc
                    }
                });
            })
            .WithName("GetOperationalQueuePressureSummary")
            .WithTags("Operational Store")
            .WithSummary("Gets a SQL-backed operational queue pressure summary.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static class SqlQueuePressureSnapshotReader
    {
        private static readonly string[] ConnectionStringNames =
        {
            "MigrationOperationalStore",
            "OperationalSql",
            "DefaultConnection"
        };

        public static async Task<QueuePressureSnapshot> ReadAsync(IConfiguration configuration, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var generatedAtUtc = DateTimeOffset.UtcNow;
            var connectionString = ResolveConnectionString(configuration);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return QueuePressureSnapshot.Unavailable(
                    generatedAtUtc,
                    "configuration",
                    "No SQL connection string was found. Checked ConnectionStrings:MigrationOperationalStore, ConnectionStrings:OperationalSql, ConnectionStrings:DefaultConnection, SqlOperationalStore:ConnectionString, and ConnectionStrings__* equivalents.");
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var hasWorkItems = await TableExistsAsync(connection, "migration", "WorkItems", cancellationToken).ConfigureAwait(false);
                if (!hasWorkItems)
                {
                    return QueuePressureSnapshot.Unavailable(generatedAtUtc, "migration.WorkItems", "Table migration.WorkItems does not exist.");
                }

                var workItemColumns = await LoadColumnsAsync(connection, "migration", "WorkItems", cancellationToken).ConfigureAwait(false);
                var runsColumns = await LoadColumnsAsync(connection, "migration", "Runs", cancellationToken).ConfigureAwait(false);

                var statusCounts = await ReadStatusCountsAsync(connection, workItemColumns, cancellationToken).ConfigureAwait(false);
                var queued = SumStatuses(statusCounts, "Queued", "Pending", "Ready", "New");
                var running = SumStatuses(statusCounts, "Running", "InProgress", "Processing", "Leased", "Started", "Executing");
                var retry = SumStatuses(statusCounts, "Retry", "Retryable", "RetryPending", "ScheduledRetry");
                var failed = SumStatuses(statusCounts, "Failed", "Error", "DeadLettered", "Poison");
                var completed = SumStatuses(statusCounts, "Completed", "Succeeded", "Success", "Done");

                var oldestQueuedUtc = await ReadOldestUtcAsync(connection, workItemColumns, "Queued", cancellationToken).ConfigureAwait(false);
                var oldestRunningUtc = await ReadOldestRunningUtcAsync(connection, workItemColumns, cancellationToken).ConfigureAwait(false);
                var newestCompletedUtc = await ReadNewestCompletedUtcAsync(connection, workItemColumns, cancellationToken).ConfigureAwait(false);
                var staleRunningDepth = await ReadStaleRunningCountAsync(connection, workItemColumns, generatedAtUtc.AddMinutes(-30), cancellationToken).ConfigureAwait(false);
                var recentRuns = await ReadRecentRunsAsync(connection, runsColumns, cancellationToken).ConfigureAwait(false);

                return new QueuePressureSnapshot(
                    Available: true,
                    Source: "migration.WorkItems",
                    Error: null,
                    GeneratedAtUtc: generatedAtUtc,
                    QueueDepth: queued,
                    RunningDepth: running,
                    RetryDepth: retry,
                    FailedDepth: failed,
                    CompletedDepth: completed,
                    StaleRunningDepth: staleRunningDepth,
                    OldestQueuedUtc: oldestQueuedUtc,
                    OldestRunningUtc: oldestRunningUtc,
                    NewestCompletedUtc: newestCompletedUtc,
                    StatusCounts: statusCounts,
                    RecentRuns: recentRuns);
            }
            catch (Exception ex)
            {
                return QueuePressureSnapshot.Unavailable(generatedAtUtc, "sql", ex.Message);
            }
        }

        private static string? ResolveConnectionString(IConfiguration configuration)
        {
            foreach (var name in ConnectionStringNames)
            {
                var value = configuration.GetConnectionString(name);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return FirstNonEmpty(
                configuration["SqlOperationalStore:ConnectionString"],
                configuration["ConnectionStrings:MigrationOperationalStore"],
                configuration["ConnectionStrings:OperationalSql"],
                configuration["ConnectionStrings:DefaultConnection"],
                configuration["ConnectionStrings__MigrationOperationalStore"],
                configuration["ConnectionStrings__OperationalSql"],
                configuration["ConnectionStrings__DefaultConnection"],
                configuration["MIGRATION_ConnectionStrings__MigrationOperationalStore"],
                configuration["MIGRATION_ConnectionStrings__OperationalSql"]);
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static async Task<bool> TableExistsAsync(SqlConnection connection, string schema, string table, CancellationToken cancellationToken)
        {
            const string sql = "select convert(bit, case when object_id(@qualifiedName, 'U') is null then 0 else 1 end);";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@qualifiedName", $"{schema}.{table}");
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is bool value && value;
        }

        private static async Task<IReadOnlySet<string>> LoadColumnsAsync(SqlConnection connection, string schema, string table, CancellationToken cancellationToken)
        {
            const string sql = @"
select c.name
from sys.columns c
join sys.objects o on o.object_id = c.object_id
join sys.schemas s on s.schema_id = o.schema_id
where s.name = @schema and o.name = @table and o.type = 'U';";

            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@schema", schema);
            command.Parameters.AddWithValue("@table", table);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                columns.Add(reader.GetString(0));
            }

            return columns;
        }

        private static async Task<IReadOnlyList<QueueStatusCount>> ReadStatusCountsAsync(SqlConnection connection, IReadOnlySet<string> columns, CancellationToken cancellationToken)
        {
            if (!columns.Contains("Status"))
            {
                return Array.Empty<QueueStatusCount>();
            }

            const string sql = @"
select cast(coalesce(Status, 'Unknown') as nvarchar(128)) as Status, count_big(*) as ItemCount
from migration.WorkItems
group by cast(coalesce(Status, 'Unknown') as nvarchar(128))
order by ItemCount desc, Status;";

            var results = new List<QueueStatusCount>();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                results.Add(new QueueStatusCount(reader.GetString(0), reader.GetInt64(1)));
            }

            return results;
        }

        private static async Task<DateTimeOffset?> ReadOldestUtcAsync(SqlConnection connection, IReadOnlySet<string> columns, string status, CancellationToken cancellationToken)
        {
            if (!columns.Contains("Status"))
            {
                return null;
            }

            var dateColumn = PickColumn(columns, "CreatedUtc", "CreatedAtUtc", "QueuedAtUtc", "UpdatedUtc", "StartedAtUtc");
            if (dateColumn is null)
            {
                return null;
            }

            var sql = $"select min([{dateColumn}]) from migration.WorkItems where Status = @status;";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@status", status);
            return ConvertToDateTimeOffset(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
        }

        private static async Task<DateTimeOffset?> ReadOldestRunningUtcAsync(SqlConnection connection, IReadOnlySet<string> columns, CancellationToken cancellationToken)
        {
            if (!columns.Contains("Status"))
            {
                return null;
            }

            var dateColumn = PickColumn(columns, "StartedAtUtc", "UpdatedUtc", "CreatedUtc", "CreatedAtUtc");
            if (dateColumn is null)
            {
                return null;
            }

            var sql = $"select min([{dateColumn}]) from migration.WorkItems where Status in ('Running', 'InProgress', 'Processing', 'Leased', 'Started', 'Executing');";
            await using var command = new SqlCommand(sql, connection);
            return ConvertToDateTimeOffset(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
        }

        private static async Task<DateTimeOffset?> ReadNewestCompletedUtcAsync(SqlConnection connection, IReadOnlySet<string> columns, CancellationToken cancellationToken)
        {
            var dateColumn = PickColumn(columns, "CompletedAtUtc", "UpdatedUtc", "FinishedAtUtc");
            if (dateColumn is null)
            {
                return null;
            }

            var statusFilter = columns.Contains("Status") ? " where Status in ('Completed', 'Succeeded', 'Success', 'Done')" : string.Empty;
            var sql = $"select max([{dateColumn}]) from migration.WorkItems{statusFilter};";
            await using var command = new SqlCommand(sql, connection);
            return ConvertToDateTimeOffset(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
        }

        private static async Task<long> ReadStaleRunningCountAsync(SqlConnection connection, IReadOnlySet<string> columns, DateTimeOffset staleBeforeUtc, CancellationToken cancellationToken)
        {
            if (!columns.Contains("Status"))
            {
                return 0;
            }

            var dateColumn = PickColumn(columns, "StartedAtUtc", "UpdatedUtc", "CreatedUtc", "CreatedAtUtc");
            if (dateColumn is null)
            {
                return 0;
            }

            var sql = $@"
select count_big(*)
from migration.WorkItems
where Status in ('Running', 'InProgress', 'Processing', 'Leased', 'Started', 'Executing')
  and [{dateColumn}] < @staleBeforeUtc;";
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@staleBeforeUtc", staleBeforeUtc.UtcDateTime);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static async Task<IReadOnlyList<RecentRunPressure>> ReadRecentRunsAsync(SqlConnection connection, IReadOnlySet<string> columns, CancellationToken cancellationToken)
        {
            if (!columns.Contains("RunKey") && !columns.Contains("RunId"))
            {
                return Array.Empty<RecentRunPressure>();
            }

            var keyColumn = columns.Contains("RunKey") ? "RunKey" : "RunId";
            var statusColumn = columns.Contains("Status") ? "Status" : null;
            var startedColumn = PickColumn(columns, "StartedAtUtc", "CreatedUtc", "CreatedAtUtc");
            var completedColumn = PickColumn(columns, "CompletedAtUtc", "FinishedAtUtc", "UpdatedUtc");
            var sourceColumn = PickColumn(columns, "SourceSystem", "Source", "SourceConnector");
            var targetColumn = PickColumn(columns, "TargetSystem", "Target", "TargetConnector");
            var orderColumn = completedColumn ?? startedColumn ?? keyColumn;

            var sql = $@"
select top (10)
    cast([{keyColumn}] as nvarchar(128)) as RunKey,
    {(statusColumn is null ? "cast(null as nvarchar(128))" : $"cast([{statusColumn}] as nvarchar(128))")} as Status,
    {(sourceColumn is null ? "cast(null as nvarchar(128))" : $"cast([{sourceColumn}] as nvarchar(128))")} as SourceSystem,
    {(targetColumn is null ? "cast(null as nvarchar(128))" : $"cast([{targetColumn}] as nvarchar(128))")} as TargetSystem,
    {(startedColumn is null ? "cast(null as datetime2)" : $"[{startedColumn}]")} as StartedUtc,
    {(completedColumn is null ? "cast(null as datetime2)" : $"[{completedColumn}]")} as CompletedUtc
from migration.Runs
order by [{orderColumn}] desc;";

            var results = new List<RecentRunPressure>();
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                results.Add(new RecentRunPressure(
                    RunKey: reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    Status: reader.IsDBNull(1) ? null : reader.GetString(1),
                    SourceSystem: reader.IsDBNull(2) ? null : reader.GetString(2),
                    TargetSystem: reader.IsDBNull(3) ? null : reader.GetString(3),
                    StartedUtc: reader.IsDBNull(4) ? null : ConvertToDateTimeOffset(reader.GetValue(4)),
                    CompletedUtc: reader.IsDBNull(5) ? null : ConvertToDateTimeOffset(reader.GetValue(5))));
            }

            return results;
        }

        private static string? PickColumn(IReadOnlySet<string> columns, params string[] names)
        {
            foreach (var name in names)
            {
                if (columns.Contains(name))
                {
                    return name;
                }
            }

            return null;
        }

        private static long SumStatuses(IReadOnlyList<QueueStatusCount> counts, params string[] statuses)
        {
            var set = new HashSet<string>(statuses, StringComparer.OrdinalIgnoreCase);
            var total = 0L;
            foreach (var count in counts)
            {
                if (set.Contains(count.Status))
                {
                    total += count.ItemCount;
                }
            }

            return total;
        }

        private static DateTimeOffset? ConvertToDateTimeOffset(object? value)
        {
            if (value is null || value is DBNull)
            {
                return null;
            }

            if (value is DateTimeOffset offset)
            {
                return offset;
            }

            if (value is DateTime dateTime)
            {
                return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
            }

            return null;
        }
    }

    private sealed record QueuePressureSnapshot(
        bool Available,
        string Source,
        string? Error,
        DateTimeOffset GeneratedAtUtc,
        long QueueDepth,
        long RunningDepth,
        long RetryDepth,
        long FailedDepth,
        long CompletedDepth,
        long StaleRunningDepth,
        DateTimeOffset? OldestQueuedUtc,
        DateTimeOffset? OldestRunningUtc,
        DateTimeOffset? NewestCompletedUtc,
        IReadOnlyList<QueueStatusCount> StatusCounts,
        IReadOnlyList<RecentRunPressure> RecentRuns)
    {
        public static QueuePressureSnapshot Unavailable(DateTimeOffset generatedAtUtc, string source, string error)
        {
            return new QueuePressureSnapshot(
                Available: false,
                Source: source,
                Error: error,
                GeneratedAtUtc: generatedAtUtc,
                QueueDepth: 0,
                RunningDepth: 0,
                RetryDepth: 0,
                FailedDepth: 0,
                CompletedDepth: 0,
                StaleRunningDepth: 0,
                OldestQueuedUtc: null,
                OldestRunningUtc: null,
                NewestCompletedUtc: null,
                StatusCounts: Array.Empty<QueueStatusCount>(),
                RecentRuns: Array.Empty<RecentRunPressure>());
        }
    }

    private sealed record QueueStatusCount(string Status, long ItemCount);

    private sealed record RecentRunPressure(
        string RunKey,
        string? Status,
        string? SourceSystem,
        string? TargetSystem,
        DateTimeOffset? StartedUtc,
        DateTimeOffset? CompletedUtc);
}
