using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Endpoints.Operational.Workers;

public static class OperationalWorkerTelemetryEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalWorkerTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/operational/workers")
            .WithTags("Operational Workers");

        group.MapGet("/telemetry", GetTelemetryAsync)
            .WithName("GetOperationalWorkerTelemetry")
            .WithSummary("Returns current worker, queue, and lease telemetry for the SQL-backed runtime.");

        group.MapGet("/leases", GetLeasesAsync)
            .WithName("GetOperationalWorkerLeases")
            .WithSummary("Returns current worker lease ownership and stale lease projections.");

        return app;
    }

    private static async Task<IResult> GetTelemetryAsync(
        IConfiguration configuration,
        [FromQuery] string? runId = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var connectionString = ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Results.Ok(new OperationalWorkerTelemetryResponse(
                GeneratedUtc: now,
                RunId: runId,
                Workers: Array.Empty<OperationalWorkerTelemetryItem>(),
                Queue: OperationalWorkerQueueTelemetry.Empty,
                Warnings: new[] { "Operational SQL connection string is not configured." }));
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var workItemsAvailable = await TableExistsAsync(connection, "migration", "WorkItems", cancellationToken);
            if (!workItemsAvailable)
            {
                return Results.Ok(new OperationalWorkerTelemetryResponse(
                    GeneratedUtc: now,
                    RunId: runId,
                    Workers: Array.Empty<OperationalWorkerTelemetryItem>(),
                    Queue: OperationalWorkerQueueTelemetry.Empty,
                    Warnings: new[] { "migration.WorkItems does not exist in the configured operational database." }));
            }

            var columns = await ReadColumnsAsync(connection, "migration", "WorkItems", cancellationToken);
            var statusColumn = PickColumn(columns, "Status");
            var runColumn = PickColumn(columns, "RunKey", "RunId", "MigrationRunId");
            var startedColumn = PickColumn(columns, "StartedAtUtc", "StartedUtc", "LeasedAtUtc", "UpdatedUtc");
            var completedColumn = PickColumn(columns, "CompletedAtUtc", "CompletedUtc");
            var workerColumn = PickColumn(columns, "WorkerId", "LockedBy", "LeaseOwner", "ExecutorId", "ProcessorId");

            if (statusColumn is null)
            {
                return Results.Ok(new OperationalWorkerTelemetryResponse(
                    GeneratedUtc: now,
                    RunId: runId,
                    Workers: Array.Empty<OperationalWorkerTelemetryItem>(),
                    Queue: OperationalWorkerQueueTelemetry.Empty,
                    Warnings: new[] { "migration.WorkItems is present, but no Status column was found." }));
            }

            var queue = await ReadQueueTelemetryAsync(connection, statusColumn, runColumn, runId, cancellationToken);
            var workers = await ReadWorkerTelemetryAsync(
                connection,
                statusColumn,
                runColumn,
                startedColumn,
                completedColumn,
                workerColumn,
                runId,
                now,
                cancellationToken);

            var warnings = new List<string>();
            if (workerColumn is null)
            {
                warnings.Add("Worker ownership columns were not found; active worker rows are synthesized from running work item state.");
            }

            return Results.Ok(new OperationalWorkerTelemetryResponse(
                GeneratedUtc: now,
                RunId: runId,
                Workers: workers,
                Queue: queue,
                Warnings: warnings));
        }
        catch (Exception ex)
        {
            return Results.Ok(new OperationalWorkerTelemetryResponse(
                GeneratedUtc: now,
                RunId: runId,
                Workers: Array.Empty<OperationalWorkerTelemetryItem>(),
                Queue: OperationalWorkerQueueTelemetry.Empty,
                Warnings: new[] { ex.Message }));
        }
    }

    private static async Task<IResult> GetLeasesAsync(
        IConfiguration configuration,
        [FromQuery] string? runId = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var connectionString = ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Results.Ok(new OperationalWorkerLeaseResponse(
                GeneratedUtc: now,
                RunId: runId,
                Leases: Array.Empty<OperationalWorkerLeaseItem>(),
                Warnings: new[] { "Operational SQL connection string is not configured." }));
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var workItemsAvailable = await TableExistsAsync(connection, "migration", "WorkItems", cancellationToken);
            if (!workItemsAvailable)
            {
                return Results.Ok(new OperationalWorkerLeaseResponse(
                    GeneratedUtc: now,
                    RunId: runId,
                    Leases: Array.Empty<OperationalWorkerLeaseItem>(),
                    Warnings: new[] { "migration.WorkItems does not exist in the configured operational database." }));
            }

            var columns = await ReadColumnsAsync(connection, "migration", "WorkItems", cancellationToken);
            var statusColumn = PickColumn(columns, "Status");
            var runColumn = PickColumn(columns, "RunKey", "RunId", "MigrationRunId");
            var startedColumn = PickColumn(columns, "StartedAtUtc", "StartedUtc", "LeasedAtUtc", "UpdatedUtc");
            var workerColumn = PickColumn(columns, "WorkerId", "LockedBy", "LeaseOwner", "ExecutorId", "ProcessorId");

            if (statusColumn is null || startedColumn is null)
            {
                return Results.Ok(new OperationalWorkerLeaseResponse(
                    GeneratedUtc: now,
                    RunId: runId,
                    Leases: Array.Empty<OperationalWorkerLeaseItem>(),
                    Warnings: new[] { "migration.WorkItems needs Status and StartedAtUtc-compatible columns before lease projections can be generated." }));
            }

            var leases = await ReadLeaseTelemetryAsync(
                connection,
                statusColumn,
                runColumn,
                startedColumn,
                workerColumn,
                runId,
                now,
                cancellationToken);

            return Results.Ok(new OperationalWorkerLeaseResponse(
                GeneratedUtc: now,
                RunId: runId,
                Leases: leases,
                Warnings: Array.Empty<string>()));
        }
        catch (Exception ex)
        {
            return Results.Ok(new OperationalWorkerLeaseResponse(
                GeneratedUtc: now,
                RunId: runId,
                Leases: Array.Empty<OperationalWorkerLeaseItem>(),
                Warnings: new[] { ex.Message }));
        }
    }

    private static async Task<OperationalWorkerQueueTelemetry> ReadQueueTelemetryAsync(
        SqlConnection connection,
        string statusColumn,
        string? runColumn,
        string? runId,
        CancellationToken cancellationToken)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = $@"
select cast([{statusColumn}] as nvarchar(128)) as StatusValue, count_big(1) as ItemCount
from [migration].[WorkItems]
{BuildRunFilter(runColumn, runId)}
group by cast([{statusColumn}] as nvarchar(128));";
        AddRunParameter(command, runColumn, runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var status = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var count = Convert.ToInt32(reader.GetInt64(1));
            counts[status] = count;
        }

        return new OperationalWorkerQueueTelemetry(
            Ready: SumStatuses(counts, "Queued", "Ready", "Pending", "Created"),
            Leased: SumStatuses(counts, "Leased", "Claimed", "Dispatched"),
            InFlight: SumStatuses(counts, "Running", "InProgress", "Processing", "Started"),
            Failed: SumStatuses(counts, "Failed", "Faulted", "Error"),
            Completed: SumStatuses(counts, "Completed", "Succeeded", "Success"),
            Retryable: SumStatuses(counts, "Retryable", "RetryPending", "RetryScheduled"));
    }

    private static async Task<IReadOnlyList<OperationalWorkerTelemetryItem>> ReadWorkerTelemetryAsync(
        SqlConnection connection,
        string statusColumn,
        string? runColumn,
        string? startedColumn,
        string? completedColumn,
        string? workerColumn,
        string? runId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = new List<OperationalWorkerTelemetryItem>();
        if (workerColumn is null && startedColumn is null)
        {
            return rows;
        }

        await using var command = connection.CreateCommand();
        if (workerColumn is not null)
        {
            var completedPredicate = completedColumn is null ? "1 = 1" : $"[{completedColumn}] is null";
            command.CommandText = $@"
select top (100)
    coalesce(nullif(cast([{workerColumn}] as nvarchar(256)), N''), N'unassigned') as WorkerId,
    max({BuildDateExpression(startedColumn)}) as LastHeartbeatUtc,
    sum(case when [{statusColumn}] in (N'Running', N'InProgress', N'Processing', N'Started') then 1 else 0 end) as InFlightWorkItems
from [migration].[WorkItems]
where {completedPredicate}
{BuildRunFilter(runColumn, runId, prependAnd: true)}
group by coalesce(nullif(cast([{workerColumn}] as nvarchar(256)), N''), N'unassigned')
order by LastHeartbeatUtc desc;";
        }
        else
        {
            command.CommandText = $@"
select top (1)
    N'sql-runtime' as WorkerId,
    max({BuildDateExpression(startedColumn)}) as LastHeartbeatUtc,
    sum(case when [{statusColumn}] in (N'Running', N'InProgress', N'Processing', N'Started') then 1 else 0 end) as InFlightWorkItems
from [migration].[WorkItems]
where [{statusColumn}] in (N'Running', N'InProgress', N'Processing', N'Started')
{BuildRunFilter(runColumn, runId, prependAnd: true)};";
        }

        AddRunParameter(command, runColumn, runId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var workerId = reader.IsDBNull(0) ? "unknown" : reader.GetString(0);
            var lastHeartbeat = reader.IsDBNull(1)
                ? now
                : new DateTimeOffset(reader.GetDateTime(1), TimeSpan.Zero);
            var inFlight = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2));
            var ageSeconds = Math.Max(0, Convert.ToInt32((now - lastHeartbeat).TotalSeconds));
            var status = ageSeconds > 300 ? "stale" : inFlight > 0 ? "active" : "idle";
            rows.Add(new OperationalWorkerTelemetryItem(
                WorkerId: workerId,
                Status: status,
                LastHeartbeatUtc: lastHeartbeat,
                ActiveLeases: inFlight,
                InFlightWorkItems: inFlight,
                Role: workerColumn is null ? "SQL runtime projection" : "SQL work item owner"));
        }

        return rows;
    }

    private static async Task<IReadOnlyList<OperationalWorkerLeaseItem>> ReadLeaseTelemetryAsync(
        SqlConnection connection,
        string statusColumn,
        string? runColumn,
        string startedColumn,
        string? workerColumn,
        string? runId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = new List<OperationalWorkerLeaseItem>();
        await using var command = connection.CreateCommand();
        var workerExpression = workerColumn is null
            ? "N'sql-runtime'"
            : $"coalesce(nullif(cast([{workerColumn}] as nvarchar(256)), N''), N'unassigned')";
        command.CommandText = $@"
select top (100)
    {workerExpression} as WorkerId,
    min([{startedColumn}]) as OldestStartedUtc,
    count_big(1) as InFlightWorkItems
from [migration].[WorkItems]
where [{statusColumn}] in (N'Running', N'InProgress', N'Processing', N'Started', N'Leased', N'Claimed', N'Dispatched')
{BuildRunFilter(runColumn, runId, prependAnd: true)}
group by {workerExpression}
order by OldestStartedUtc asc;";
        AddRunParameter(command, runColumn, runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var workerId = reader.IsDBNull(0) ? "unknown" : reader.GetString(0);
            var oldest = reader.IsDBNull(1) ? now : new DateTimeOffset(reader.GetDateTime(1), TimeSpan.Zero);
            var inFlight = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetInt64(2));
            var ageSeconds = Math.Max(0, Convert.ToInt32((now - oldest).TotalSeconds));
            var status = ageSeconds > 900 ? "stale" : "active";
            var secondsRemaining = Math.Max(0, 900 - ageSeconds);
            rows.Add(new OperationalWorkerLeaseItem(
                LeaseId: $"work-items:{workerId}",
                WorkerId: workerId,
                Status: status,
                ExpiresUtc: now.AddSeconds(secondsRemaining),
                SecondsRemaining: secondsRemaining,
                InFlightWorkItems: inFlight));
        }

        return rows;
    }

    private static string? ResolveConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("MigrationOperationalStore")
            ?? configuration.GetConnectionString("OperationalSql")
            ?? configuration.GetConnectionString("SqlOperationalStore")
            ?? configuration["ConnectionStrings:MigrationOperationalStore"]
            ?? configuration["ConnectionStrings:OperationalSql"]
            ?? configuration["ConnectionStrings:SqlOperationalStore"]
            ?? configuration["SqlOperationalStore:ConnectionString"]
            ?? configuration["OperationalSql:ConnectionString"];
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string schema, string table, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
select count_big(1)
from sys.tables t
join sys.schemas s on s.schema_id = t.schema_id
where s.name = @SchemaName and t.name = @TableName;";
        command.Parameters.Add(new SqlParameter("@SchemaName", SqlDbType.NVarChar, 128) { Value = schema });
        command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 128) { Value = table });
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result) > 0;
    }

    private static async Task<IReadOnlySet<string>> ReadColumnsAsync(SqlConnection connection, string schema, string table, CancellationToken cancellationToken)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
select c.name
from sys.columns c
join sys.tables t on t.object_id = c.object_id
join sys.schemas s on s.schema_id = t.schema_id
where s.name = @SchemaName and t.name = @TableName;";
        command.Parameters.Add(new SqlParameter("@SchemaName", SqlDbType.NVarChar, 128) { Value = schema });
        command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 128) { Value = table });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
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

    private static string BuildRunFilter(string? runColumn, string? runId, bool prependAnd = false)
    {
        if (runColumn is null || string.IsNullOrWhiteSpace(runId))
        {
            return string.Empty;
        }

        return prependAnd
            ? $"and cast([{runColumn}] as nvarchar(256)) = @RunId"
            : $"where cast([{runColumn}] as nvarchar(256)) = @RunId";
    }

    private static void AddRunParameter(SqlCommand command, string? runColumn, string? runId)
    {
        if (runColumn is not null && !string.IsNullOrWhiteSpace(runId))
        {
            command.Parameters.Add(new SqlParameter("@RunId", SqlDbType.NVarChar, 256) { Value = runId });
        }
    }

    private static string BuildDateExpression(string? column)
    {
        return column is null ? "sysutcdatetime()" : $"[{column}]";
    }

    private static int SumStatuses(IReadOnlyDictionary<string, int> counts, params string[] statuses)
    {
        var total = 0;
        foreach (var status in statuses)
        {
            if (counts.TryGetValue(status, out var count))
            {
                total += count;
            }
        }

        return total;
    }
}

public sealed record OperationalWorkerTelemetryResponse(
    DateTimeOffset GeneratedUtc,
    string? RunId,
    IReadOnlyCollection<OperationalWorkerTelemetryItem> Workers,
    OperationalWorkerQueueTelemetry Queue,
    IReadOnlyCollection<string> Warnings);

public sealed record OperationalWorkerTelemetryItem(
    string WorkerId,
    string Status,
    DateTimeOffset LastHeartbeatUtc,
    int ActiveLeases,
    int InFlightWorkItems,
    string Role);

public sealed record OperationalWorkerQueueTelemetry(
    int Ready,
    int Leased,
    int InFlight,
    int Failed,
    int Completed,
    int Retryable)
{
    public static OperationalWorkerQueueTelemetry Empty { get; } = new(0, 0, 0, 0, 0, 0);
}

public sealed record OperationalWorkerLeaseResponse(
    DateTimeOffset GeneratedUtc,
    string? RunId,
    IReadOnlyCollection<OperationalWorkerLeaseItem> Leases,
    IReadOnlyCollection<string> Warnings);

public sealed record OperationalWorkerLeaseItem(
    string LeaseId,
    string WorkerId,
    string Status,
    DateTimeOffset ExpiresUtc,
    int SecondsRemaining,
    int InFlightWorkItems);
