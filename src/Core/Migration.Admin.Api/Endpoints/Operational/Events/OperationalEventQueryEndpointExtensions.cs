using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Migration.Admin.Api.Operational.Events;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace Migration.Admin.Api.Endpoints.Operational.Events;

public static class OperationalEventQueryEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalEventQueryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/events/query")
            .WithTags("Operational Event Query");

        group.MapGet("/", async (
            IOperationalEventQueryService queryService,
            IConfiguration configuration,
            string? severity,
            string? category,
            string? eventType,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            Guid? executionSessionId,
            Guid? migrationRunId,
            int? skip,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var request = new OperationalEventQueryRequest(
                Severity: Normalize(severity),
                Category: Normalize(category),
                EventType: Normalize(eventType),
                FromUtc: fromUtc,
                ToUtc: toUtc,
                ExecutionSessionId: executionSessionId,
                MigrationRunId: migrationRunId,
                Skip: Math.Max(0, skip.GetValueOrDefault(0)),
                Take: Math.Clamp(take.GetValueOrDefault(50), 1, 250));

            var storedEvents = await queryService.QueryAsync(request, cancellationToken);
            if (storedEvents.Count > 0)
            {
                return Results.Ok(new OperationalEventQueryResponse(
                    Skip: request.Skip,
                    Take: request.Take,
                    Returned: storedEvents.Count,
                    Events: storedEvents));
            }

            var synthesizedRows = await SqlOperationalEventTimelineReader.ReadAsync(
                configuration,
                migrationRunId,
                request.Skip,
                request.Take,
                fromUtc,
                toUtc,
                cancellationToken);

            var synthesizedEvents = ToOperationalEventRecords(synthesizedRows);

            return Results.Ok(new OperationalEventQueryResponse(
                Skip: request.Skip,
                Take: request.Take,
                Returned: synthesizedEvents.Count,
                Events: synthesizedEvents));
        })
        .WithName("QueryOperationalEvents");

        group.MapGet("/summary", async (
            IOperationalEventQueryService queryService,
            IConfiguration configuration,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            CancellationToken cancellationToken) =>
        {
            var summary = await queryService.ReadAggregateSummaryAsync(
                fromUtc,
                toUtc,
                cancellationToken);

            return Results.Ok(summary);
        })
        .WithName("GetOperationalEventQuerySummary");

        return endpoints;
    }

    private static IReadOnlyList<OperationalEventRecord> ToOperationalEventRecords(IReadOnlyList<OperationalEventTimelineRow> rows)
    {
        return rows
            .Select(row => new OperationalEventRecord(
                OperationalEventId: CreateDeterministicGuid(row.EventId),
                EventType: row.EventType,
                Severity: row.Severity,
                Category: "runtime",
                Source: row.Source,
                Message: row.Message,
                PayloadJson: null,
                CreatedUtc: row.CreatedAtUtc,
                ExecutionSessionId: null,
                MigrationRunId: TryParseGuid(row.RunId)))
            .ToArray();
    }

    private static Guid CreateDeterministicGuid(string value)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }

    private static Guid? TryParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

internal static class SqlOperationalEventTimelineReader
{
    public static async Task<IReadOnlyList<OperationalEventTimelineRow>> ReadAsync(
        IConfiguration configuration,
        Guid? migrationRunId,
        int skip,
        int take,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var connectionString = ReadOperationalConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Array.Empty<OperationalEventTimelineRow>();
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var runColumns = await ReadColumnsAsync(connection, "migration", "Runs", cancellationToken);
        var workItemColumns = await ReadColumnsAsync(connection, "migration", "WorkItems", cancellationToken);

        if (runColumns.Count == 0 && workItemColumns.Count == 0)
        {
            return Array.Empty<OperationalEventTimelineRow>();
        }

        var runKeyColumn = Pick(runColumns, "RunKey", "RunId", "Id");
        var runStatusColumn = Pick(runColumns, "Status", "State");
        var runStartedColumn = Pick(runColumns, "StartedAtUtc", "StartedUtc", "CreatedUtc", "QueuedAtUtc");
        var runCompletedColumn = Pick(runColumns, "CompletedAtUtc", "CompletedUtc");
        var runReasonColumn = Pick(runColumns, "StatusReason", "Reason", "ErrorMessage");

        var workRunColumn = Pick(workItemColumns, "RunKey", "RunId", "MigrationRunId");
        var workIdColumn = Pick(workItemColumns, "WorkItemId", "Id");
        var workStatusColumn = Pick(workItemColumns, "Status", "State");
        var workStartedColumn = Pick(workItemColumns, "StartedAtUtc", "StartedUtc", "UpdatedUtc", "CreatedUtc");
        var workCompletedColumn = Pick(workItemColumns, "CompletedAtUtc", "CompletedUtc");
        var workResultColumn = Pick(workItemColumns, "ResultJson", "ErrorJson", "StatusReason", "FailureReason");

        var events = new List<OperationalEventTimelineRow>();

        if (runKeyColumn is not null && runStatusColumn is not null)
        {
            await ReadRunEventsAsync(
                connection,
                events,
                migrationRunId,
                fromUtc,
                toUtc,
                runKeyColumn,
                runStatusColumn,
                runStartedColumn,
                runCompletedColumn,
                runReasonColumn,
                cancellationToken);
        }

        if (workRunColumn is not null && workStatusColumn is not null)
        {
            await ReadWorkItemEventsAsync(
                connection,
                events,
                migrationRunId,
                fromUtc,
                toUtc,
                workRunColumn,
                workIdColumn,
                workStatusColumn,
                workStartedColumn,
                workCompletedColumn,
                workResultColumn,
                cancellationToken);
        }

        return events
            .OrderByDescending(item => item.CreatedAtUtc)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 250))
            .ToArray();
    }

    private static async Task ReadRunEventsAsync(
        SqlConnection connection,
        List<OperationalEventTimelineRow> events,
        Guid? migrationRunId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string runKeyColumn,
        string runStatusColumn,
        string? runStartedColumn,
        string? runCompletedColumn,
        string? runReasonColumn,
        CancellationToken cancellationToken)
    {
        var timestampExpression = SqlCoalesce(runCompletedColumn, runStartedColumn, "SYSUTCDATETIME()");
        var reasonExpression = runReasonColumn is null ? "CAST(NULL AS nvarchar(max))" : SqlName(runReasonColumn);

        var sql = $@"
SELECT TOP (500)
       CONVERT(nvarchar(64), {SqlName(runKeyColumn)}) AS RunId,
       CAST(NULL AS bigint) AS WorkItemId,
       CONVERT(nvarchar(64), {SqlName(runStatusColumn)}) AS Status,
       {timestampExpression} AS CreatedAtUtc,
       {reasonExpression} AS Message
FROM migration.Runs
WHERE (@RunId IS NULL OR CONVERT(nvarchar(64), {SqlName(runKeyColumn)}) = @RunId)
  AND (@FromUtc IS NULL OR {timestampExpression} >= @FromUtc)
  AND (@ToUtc IS NULL OR {timestampExpression} <= @ToUtc)
ORDER BY {timestampExpression} DESC;";

        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, migrationRunId, fromUtc, toUtc);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var status = ReadString(reader, "Status") ?? "Unknown";
            events.Add(new OperationalEventTimelineRow(
                EventId: $"run-{ReadString(reader, "RunId")}-{status}",
                RunId: ReadString(reader, "RunId"),
                WorkItemId: null,
                EventType: $"Run{status}",
                Severity: SeverityFor(status),
                Message: ReadString(reader, "Message") ?? $"Run status is {status}.",
                Source: "migration.Runs",
                CreatedAtUtc: ReadDateTimeOffset(reader, "CreatedAtUtc")));
        }
    }

    private static async Task ReadWorkItemEventsAsync(
        SqlConnection connection,
        List<OperationalEventTimelineRow> events,
        Guid? migrationRunId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string workRunColumn,
        string? workIdColumn,
        string workStatusColumn,
        string? workStartedColumn,
        string? workCompletedColumn,
        string? workResultColumn,
        CancellationToken cancellationToken)
    {
        var timestampExpression = SqlCoalesce(workCompletedColumn, workStartedColumn, "SYSUTCDATETIME()");
        var workIdExpression = workIdColumn is null ? "CAST(NULL AS bigint)" : $"TRY_CONVERT(bigint, {SqlName(workIdColumn)})";
        var resultExpression = workResultColumn is null ? "CAST(NULL AS nvarchar(max))" : $"CONVERT(nvarchar(max), {SqlName(workResultColumn)})";

        var sql = $@"
SELECT TOP (1000)
       CONVERT(nvarchar(64), {SqlName(workRunColumn)}) AS RunId,
       {workIdExpression} AS WorkItemId,
       CONVERT(nvarchar(64), {SqlName(workStatusColumn)}) AS Status,
       {timestampExpression} AS CreatedAtUtc,
       {resultExpression} AS Message
FROM migration.WorkItems
WHERE (@RunId IS NULL OR CONVERT(nvarchar(64), {SqlName(workRunColumn)}) = @RunId)
  AND (@FromUtc IS NULL OR {timestampExpression} >= @FromUtc)
  AND (@ToUtc IS NULL OR {timestampExpression} <= @ToUtc)
ORDER BY {timestampExpression} DESC;";

        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, migrationRunId, fromUtc, toUtc);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var status = ReadString(reader, "Status") ?? "Unknown";
            var workItemId = ReadNullableLong(reader, "WorkItemId");
            events.Add(new OperationalEventTimelineRow(
                EventId: $"workitem-{ReadString(reader, "RunId")}-{workItemId?.ToString() ?? "unknown"}-{status}",
                RunId: ReadString(reader, "RunId"),
                WorkItemId: workItemId,
                EventType: $"WorkItem{status}",
                Severity: SeverityFor(status),
                Message: BuildWorkItemMessage(status, workItemId, ReadString(reader, "Message")),
                Source: "migration.WorkItems",
                CreatedAtUtc: ReadDateTimeOffset(reader, "CreatedAtUtc")));
        }
    }

    private static void AddParameters(SqlCommand command, Guid? migrationRunId, DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        command.Parameters.Add(new SqlParameter("@RunId", SqlDbType.NVarChar, 64) { Value = migrationRunId?.ToString() ?? (object)DBNull.Value });
        command.Parameters.Add(new SqlParameter("@FromUtc", SqlDbType.DateTimeOffset) { Value = fromUtc ?? (object)DBNull.Value });
        command.Parameters.Add(new SqlParameter("@ToUtc", SqlDbType.DateTimeOffset) { Value = toUtc ?? (object)DBNull.Value });
    }

    private static async Task<HashSet<string>> ReadColumnsAsync(SqlConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT c.name
FROM sys.columns AS c
INNER JOIN sys.tables AS t ON c.object_id = t.object_id
INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
WHERE s.name = @SchemaName AND t.name = @TableName;";

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@SchemaName", SqlDbType.NVarChar, 128) { Value = schemaName });
        command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 128) { Value = tableName });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private static string? ReadOperationalConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("MigrationOperationalStore")
            ?? configuration.GetConnectionString("OperationalSql")
            ?? configuration["ConnectionStrings:MigrationOperationalStore"]
            ?? configuration["ConnectionStrings:OperationalSql"]
            ?? configuration["SqlOperationalStore:ConnectionString"];
    }

    private static string? Pick(HashSet<string> columns, params string[] names)
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

    private static string SqlName(string name)
    {
        return "[" + name.Replace("]", "]]", StringComparison.Ordinal) + "]";
    }

    private static string SqlCoalesce(string? firstColumn, string? secondColumn, string fallbackExpression)
    {
        var expressions = new List<string>();
        if (firstColumn is not null) { expressions.Add(SqlName(firstColumn)); }
        if (secondColumn is not null) { expressions.Add(SqlName(secondColumn)); }
        expressions.Add(fallbackExpression);
        return "COALESCE(" + string.Join(", ", expressions) + ")";
    }

    private static string? ReadString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static long? ReadNullableLong(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return Convert.ToInt64(reader.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ReadDateTimeOffset(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return DateTimeOffset.UtcNow;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset offset => offset,
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => DateTimeOffset.UtcNow
        };
    }

    private static string SeverityFor(string status)
    {
        if (status.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            return "error";
        }

        if (status.Contains("retry", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("cancel", StringComparison.OrdinalIgnoreCase))
        {
            return "warning";
        }

        return "info";
    }

    private static string BuildWorkItemMessage(string status, long? workItemId, string? detail)
    {
        var prefix = workItemId.HasValue
            ? $"Work item {workItemId.Value} status is {status}."
            : $"Work item status is {status}.";

        if (string.IsNullOrWhiteSpace(detail))
        {
            return prefix;
        }

        return detail.Length > 500
            ? prefix + " " + detail[..500]
            : prefix + " " + detail;
    }
}

public sealed record OperationalEventTimelineRow(
    string EventId,
    string? RunId,
    long? WorkItemId,
    string EventType,
    string Severity,
    string Message,
    string Source,
    DateTimeOffset CreatedAtUtc);
