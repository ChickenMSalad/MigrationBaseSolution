using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text.Json;

namespace Migration.Admin.Api.Endpoints.Operational.Dashboard;

public static class RuntimeDashboardDetailEndpointExtensions
{
    public static IEndpointRouteBuilder MapSqlOperationalRuntimeDashboardDetailEndpoints(
        this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app
            .MapGroup("/api/runtime/dashboard")
            .WithTags("Runtime Dashboard");

        group.MapGet("/runs/{runId}", GetRunDetailAsync)
            .WithName("GetRuntimeDashboardRunDetail");

        group.MapGet("/runs/{runId}/work-items", GetRunWorkItemsAsync)
            .WithName("GetRuntimeDashboardRunWorkItems");

        group.MapGet("/runs/{runId}/failures", GetRunFailuresAsync)
            .WithName("GetRuntimeDashboardRunFailures");

        group.MapGet("/runs/{runId}/events", GetRunEventsAsync)
            .WithName("GetRuntimeDashboardRunEvents");

        group.MapGet("/failures", GetAllFailuresAsync)
            .WithName("GetRuntimeDashboardFailures");

        return app;
    }

    private static async Task<IResult> GetRunDetailAsync(
        string runId,
        IConfiguration configuration,
        int? take,
        CancellationToken cancellationToken)
    {
        var rowLimit = Math.Clamp(take.GetValueOrDefault(100), 1, 500);
        var connectionString = ResolveConnectionString(configuration);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var resolvedRunId = await ResolveRunIdAsync(connection, runId, cancellationToken).ConfigureAwait(false);
        if (resolvedRunId is null)
        {
            return Results.NotFound(new { runId });
        }

        var run = await ReadRunAsync(connection, resolvedRunId.Value, cancellationToken).ConfigureAwait(false);
        if (run is null)
        {
            return Results.NotFound(new { runId });
        }

        var workItems = await ReadWorkItemsAsync(connection, resolvedRunId.Value, rowLimit, failedOnly: false, cancellationToken).ConfigureAwait(false);
        var failures = await ReadFailureItemsAsync(connection, resolvedRunId.Value, rowLimit, cancellationToken).ConfigureAwait(false);
        var events = await ReadRunEventsAsync(connection, resolvedRunId.Value, rowLimit, cancellationToken).ConfigureAwait(false);
        var progress = BuildProgressSnapshot(events);

        return Results.Ok(new
        {
            run,
            progress,
            workItems,
            failures,
            events
        });
    }

    private static async Task<IResult> GetRunWorkItemsAsync(
        string runId,
        IConfiguration configuration,
        int? take,
        CancellationToken cancellationToken)
    {
        var rowLimit = Math.Clamp(take.GetValueOrDefault(100), 1, 500);
        var connectionString = ResolveConnectionString(configuration);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var resolvedRunId = await ResolveRunIdAsync(connection, runId, cancellationToken).ConfigureAwait(false);
        if (resolvedRunId is null)
        {
            return Results.NotFound(new { runId });
        }

        return Results.Ok(await ReadWorkItemsAsync(connection, resolvedRunId.Value, rowLimit, failedOnly: false, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<IResult> GetRunFailuresAsync(
        string runId,
        IConfiguration configuration,
        int? take,
        CancellationToken cancellationToken)
    {
        var rowLimit = Math.Clamp(take.GetValueOrDefault(100), 1, 500);
        var connectionString = ResolveConnectionString(configuration);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var resolvedRunId = await ResolveRunIdAsync(connection, runId, cancellationToken).ConfigureAwait(false);
        if (resolvedRunId is null)
        {
            return Results.NotFound(new { runId });
        }

        return Results.Ok(await ReadFailureItemsAsync(connection, resolvedRunId.Value, rowLimit, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<IResult> GetRunEventsAsync(
        string runId,
        IConfiguration configuration,
        int? take,
        CancellationToken cancellationToken)
    {
        var rowLimit = Math.Clamp(take.GetValueOrDefault(100), 1, 500);
        var connectionString = ResolveConnectionString(configuration);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var resolvedRunId = await ResolveRunIdAsync(connection, runId, cancellationToken).ConfigureAwait(false);
        if (resolvedRunId is null)
        {
            return Results.NotFound(new { runId });
        }

        return Results.Ok(await ReadRunEventsAsync(connection, resolvedRunId.Value, rowLimit, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<IResult> GetAllFailuresAsync(
        IConfiguration configuration,
        int? take,
        CancellationToken cancellationToken)
    {
        var rowLimit = Math.Clamp(take.GetValueOrDefault(50), 1, 500);
        var connectionString = ResolveConnectionString(configuration);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var summary = await ReadFailureSummaryAsync(connection, cancellationToken).ConfigureAwait(false);
        var rows = await ReadFailureRowsAsync(connection, rowLimit, cancellationToken).ConfigureAwait(false);

        return Results.Ok(new
        {
            summary,
            workItems = rows,
            message = rows.Count == 0
                ? "No failed or retryable work items were found in migration.WorkItems."
                : null
        });
    }

    private static async Task<Guid?> ResolveRunIdAsync(
        SqlConnection connection,
        string runIdOrKey,
        CancellationToken cancellationToken)
    {
        if (Guid.TryParse(runIdOrKey, out var parsed))
        {
            return parsed;
        }

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = @"
SELECT TOP (1) RunId
FROM migration.Runs
WHERE RunKey = @RunKey;";
        command.Parameters.Add(new SqlParameter("@RunKey", SqlDbType.NVarChar, 256) { Value = runIdOrKey });

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (value is Guid guid)
        {
            return guid;
        }

        return null;
    }

    private static async Task<object?> ReadRunAsync(
        SqlConnection connection,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = @"
SELECT TOP (1)
    r.RunId,
    r.RunKey,
    r.RunName,
    r.SourceSystem,
    r.TargetSystem,
    r.Status,
    r.EnvironmentName,
    r.IsDryRun,
    r.RequestedAtUtc,
    r.CreatedAtUtc,
    r.UpdatedAtUtc,
    COUNT_BIG(w.WorkItemId) AS WorkItemCount,
    SUM(CASE WHEN w.Status IN (N'Pending', N'Ready', N'Queued', N'FailedRetryable') THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS QueuedWorkItemCount,
    SUM(CASE WHEN w.Status IN (N'Dispatching', N'Dispatched') THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS DispatchedWorkItemCount,
    SUM(CASE WHEN w.Status IN (N'Leased', N'Running', N'InProgress', N'Processing', N'Started', N'Executing') OR (w.StartedAtUtc IS NOT NULL AND w.CompletedAtUtc IS NULL AND w.Status NOT IN (N'Completed')) THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS RunningWorkItemCount,
    SUM(CASE WHEN w.Status = N'Completed' THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS CompletedWorkItemCount,
    SUM(CASE WHEN w.Status LIKE N'Failed%' OR w.LastErrorMessage IS NOT NULL THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS FailedWorkItemCount,
    SUM(CASE WHEN w.Status IN (N'Retryable', N'FailedRetryable') THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS RetryableWorkItemCount,
    SUM(CASE WHEN w.Status = N'RetryQueued' THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS RetryQueuedWorkItemCount,
    MIN(w.StartedAtUtc) AS FirstWorkItemStartedAtUtc,
    MAX(w.CompletedAtUtc) AS LastWorkItemCompletedAtUtc,
    (
        SELECT TOP (1) wi.PayloadJson
        FROM migration.WorkItems wi
        WHERE wi.RunId = r.RunId
        ORDER BY wi.CreatedUtc ASC
    ) AS SamplePayloadJson
FROM migration.Runs r
LEFT JOIN migration.WorkItems w ON w.RunId = r.RunId
WHERE r.RunId = @RunId
GROUP BY
    r.RunId,
    r.RunKey,
    r.RunName,
    r.SourceSystem,
    r.TargetSystem,
    r.Status,
    r.EnvironmentName,
    r.IsDryRun,
    r.RequestedAtUtc,
    r.CreatedAtUtc,
    r.UpdatedAtUtc;";
        command.Parameters.Add(new SqlParameter("@RunId", SqlDbType.UniqueIdentifier) { Value = runId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var total = ToInt64(reader["WorkItemCount"]);
        var queued = ToInt64(reader["QueuedWorkItemCount"]);
        var dispatched = ToInt64(reader["DispatchedWorkItemCount"]);
        var running = ToInt64(reader["RunningWorkItemCount"]);
        var completed = ToInt64(reader["CompletedWorkItemCount"]);
        var failed = ToInt64(reader["FailedWorkItemCount"]);
        var status = ToNullableString(reader["Status"]);

        return new
        {
            runId = reader["RunId"],
            runKey = ToNullableString(reader["RunKey"]),
            runName = ToNullableString(reader["RunName"]),
            sourceSystem = ToNullableString(reader["SourceSystem"]),
            targetSystem = ToNullableString(reader["TargetSystem"]),
            status,
            effectiveStatus = CalculateEffectiveStatus(status, total, queued, dispatched, running, completed, failed),
            environmentName = ToNullableString(reader["EnvironmentName"]),
            isDryRun = reader["IsDryRun"] is not DBNull && Convert.ToBoolean(reader["IsDryRun"]),
            overwriteExisting = ReadOverwriteExisting(reader["SamplePayloadJson"]),
            requestedAtUtc = ToNullableValue(reader["RequestedAtUtc"]),
            createdAtUtc = ToNullableValue(reader["CreatedAtUtc"]),
            updatedAtUtc = ToNullableValue(reader["UpdatedAtUtc"]),
            firstWorkItemStartedAtUtc = ToNullableValue(reader["FirstWorkItemStartedAtUtc"]),
            lastWorkItemCompletedAtUtc = ToNullableValue(reader["LastWorkItemCompletedAtUtc"]),
            workItemCount = total,
            queuedWorkItemCount = queued,
            dispatchedWorkItemCount = dispatched,
            runningWorkItemCount = running,
            completedWorkItemCount = completed,
            failedWorkItemCount = failed,
            retryableWorkItemCount = ToInt64(reader["RetryableWorkItemCount"]),
            retryQueuedWorkItemCount = ToInt64(reader["RetryQueuedWorkItemCount"]),
            activeWorkItemCount = queued + dispatched + running,
            percentComplete = CalculatePercentComplete(total, completed, failed)
        };
    }


    private static bool ReadOverwriteExisting(object? payloadJsonValue)
    {
        if (payloadJsonValue is null || payloadJsonValue is DBNull)
        {
            return false;
        }

        var payloadJson = Convert.ToString(payloadJsonValue);
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if (TryReadBoolean(root, "overwriteExisting", out var value) ||
                TryReadBoolean(root, "Overwrite", out value) ||
                TryReadBoolean(root, "overwrite", out value) ||
                TryReadBoolean(root, "AzureBlobTargetOverwrite", out value))
            {
                return value;
            }

            if (root.TryGetProperty("job", out var job) && job.ValueKind == JsonValueKind.Object &&
                job.TryGetProperty("settings", out var settings) && settings.ValueKind == JsonValueKind.Object)
            {
                return
                    (TryReadBoolean(settings, "overwriteExisting", out value) && value) ||
                    (TryReadBoolean(settings, "Overwrite", out value) && value) ||
                    (TryReadBoolean(settings, "overwrite", out value) && value) ||
                    (TryReadBoolean(settings, "AzureBlobTargetOverwrite", out value) && value);
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static bool TryReadBoolean(JsonElement element, string propertyName, out bool value)
    {
        value = false;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (property.ValueKind == JsonValueKind.False)
        {
            value = false;
            return true;
        }

        if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }


    private static async Task<List<object>> ReadRunEventsAsync(
        SqlConnection connection,
        Guid runId,
        int take,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = @"
IF OBJECT_ID(N'dbo.MigrationOperationalEvents', N'U') IS NULL
BEGIN
    SELECT TOP (0)
        CAST(NULL AS uniqueidentifier) AS OperationalEventId,
        CAST(NULL AS nvarchar(128)) AS EventType,
        CAST(NULL AS nvarchar(32)) AS Severity,
        CAST(NULL AS nvarchar(128)) AS Category,
        CAST(NULL AS nvarchar(256)) AS Source,
        CAST(NULL AS nvarchar(max)) AS Message,
        CAST(NULL AS nvarchar(max)) AS PayloadJson,
        CAST(NULL AS datetimeoffset) AS CreatedUtc;
END
ELSE
BEGIN
    SELECT TOP (@Take)
        OperationalEventId,
        EventType,
        Severity,
        Category,
        Source,
        Message,
        PayloadJson,
        CreatedUtc
    FROM dbo.MigrationOperationalEvents
    WHERE MigrationRunId = @RunId
    ORDER BY CreatedUtc DESC;
END";
        command.Parameters.Add(new SqlParameter("@RunId", SqlDbType.UniqueIdentifier) { Value = runId });
        command.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = take });

        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var payloadJson = ToNullableString(reader["PayloadJson"]);
            var progress = ParseProgressPayload(payloadJson);
            rows.Add(new
            {
                eventId = reader["OperationalEventId"],
                eventType = ToNullableString(reader["EventType"]),
                severity = ToNullableString(reader["Severity"]),
                category = ToNullableString(reader["Category"]),
                source = ToNullableString(reader["Source"]),
                message = ToNullableString(reader["Message"]),
                payloadJson,
                createdAtUtc = ToNullableValue(reader["CreatedUtc"]),
                workItemId = progress.WorkItemId,
                completed = progress.Completed,
                total = progress.Total
            });
        }

        return rows;
    }

    private static object BuildProgressSnapshot(IReadOnlyList<object> events)
    {
        foreach (var item in events)
        {
            var completed = GetPropertyValue(item, "completed");
            var total = GetPropertyValue(item, "total");
            if (completed is null || total is null)
            {
                continue;
            }

            var completedValue = Convert.ToInt32(completed, System.Globalization.CultureInfo.InvariantCulture);
            var totalValue = Convert.ToInt32(total, System.Globalization.CultureInfo.InvariantCulture);
            var createdAtUtc = GetPropertyValue(item, "createdAtUtc");
            var message = Convert.ToString(GetPropertyValue(item, "message"), System.Globalization.CultureInfo.InvariantCulture);
            var percentComplete = totalValue <= 0
                ? 0.0d
                : Math.Round(((double)completedValue / totalValue) * 100.0d, 2, MidpointRounding.AwayFromZero);

            return new
            {
                completed = completedValue,
                total = totalValue,
                percentComplete,
                message,
                updatedAtUtc = createdAtUtc
            };
        }

        return new
        {
            completed = (int?)null,
            total = (int?)null,
            percentComplete = (double?)null,
            message = (string?)null,
            updatedAtUtc = (object?)null
        };
    }

    private static (int? Completed, int? Total, string? WorkItemId) ParseProgressPayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return (null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            return (
                TryReadInt32(root, "completed", out var completed) ? completed : null,
                TryReadInt32(root, "total", out var total) ? total : null,
                TryReadString(root, "workItemId", out var workItemId) ? workItemId : null);
        }
        catch (JsonException)
        {
            return (null, null, null);
        }
    }

    private static bool TryReadInt32(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value))
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out value))
        {
            return true;
        }

        return false;
    }

    private static bool TryReadString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = property.ToString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static async Task<object> ReadFailureSummaryAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = @"
SELECT
    COUNT_BIG(CASE WHEN Status LIKE N'Failed%' OR LastErrorMessage IS NOT NULL THEN 1 END) AS Failed,
    COUNT_BIG(CASE WHEN Status IN (N'Retryable', N'FailedRetryable') THEN 1 END) AS Retryable,
    COUNT_BIG(CASE WHEN Status = N'RetryQueued' THEN 1 END) AS RetryQueued,
    MAX(COALESCE(UpdatedUtc, CreatedUtc)) AS LastUpdatedUtc
FROM migration.WorkItems
WHERE
    Status LIKE N'Failed%'
    OR Status IN (N'Retryable', N'FailedRetryable', N'RetryQueued')
    OR LastErrorMessage IS NOT NULL;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return new
            {
                failed = 0L,
                retryable = 0L,
                retryQueued = 0L,
                lastUpdatedUtc = (object?)null
            };
        }

        return new
        {
            failed = ToInt64(reader["Failed"]),
            retryable = ToInt64(reader["Retryable"]),
            retryQueued = ToInt64(reader["RetryQueued"]),
            lastUpdatedUtc = ToNullableValue(reader["LastUpdatedUtc"])
        };
    }

    private static async Task<List<object>> ReadFailureRowsAsync(
        SqlConnection connection,
        int take,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = @"
SELECT TOP (@Take)
    WorkItemId,
    RunId,
    WorkItemType,
    Status,
    AttemptCount,
    LeaseOwner,
    CreatedUtc,
    UpdatedUtc,
    CompletedAtUtc,
    LastErrorMessage
FROM migration.WorkItems
WHERE
    Status LIKE N'Failed%'
    OR Status IN (N'Retryable', N'FailedRetryable', N'RetryQueued')
    OR LastErrorMessage IS NOT NULL
ORDER BY COALESCE(UpdatedUtc, CreatedUtc) DESC, WorkItemId DESC;";
        command.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = take });

        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(MapWorkItemRow(reader));
        }

        return rows;
    }

    private static async Task<List<object>> ReadWorkItemsAsync(
        SqlConnection connection,
        Guid runId,
        int take,
        bool failedOnly,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = failedOnly
            ? @"
SELECT TOP (@Take)
    WorkItemId,
    RunId,
    WorkItemType,
    Status,
    AttemptCount,
    LeaseOwner,
    CreatedUtc,
    UpdatedUtc,
    CompletedAtUtc,
    LastErrorMessage
FROM migration.WorkItems
WHERE
    RunId = @RunId
    AND (
        Status LIKE N'Failed%'
        OR Status IN (N'Retryable', N'FailedRetryable', N'RetryQueued')
        OR LastErrorMessage IS NOT NULL
    )
ORDER BY COALESCE(UpdatedUtc, CreatedUtc) DESC, WorkItemId DESC;"
            : @"
SELECT TOP (@Take)
    WorkItemId,
    RunId,
    WorkItemType,
    Status,
    AttemptCount,
    LeaseOwner,
    CreatedUtc,
    UpdatedUtc,
    CompletedAtUtc,
    LastErrorMessage
FROM migration.WorkItems
WHERE RunId = @RunId
ORDER BY COALESCE(UpdatedUtc, CreatedUtc) DESC, WorkItemId DESC;";

        command.Parameters.Add(new SqlParameter("@RunId", SqlDbType.UniqueIdentifier) { Value = runId });
        command.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = take });

        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(MapWorkItemRow(reader));
        }

        return rows;
    }

    private static async Task<List<object>> ReadFailureItemsAsync(
        SqlConnection connection,
        Guid runId,
        int take,
        CancellationToken cancellationToken)
    {
        var rows = await ReadWorkItemsAsync(connection, runId, take, failedOnly: true, cancellationToken).ConfigureAwait(false);
        return rows.Select(row => (object)new
        {
            failureId = (string?)null,
            runId = GetPropertyValue(row, "runId"),
            workItemId = GetPropertyValue(row, "workItemId"),
            manifestRowId = (long?)null,
            failureType = GetPropertyValue(row, "status"),
            message = GetPropertyValue(row, "lastErrorMessage"),
            createdAtUtc = GetPropertyValue(row, "updatedAtUtc") ?? GetPropertyValue(row, "createdAtUtc")
        }).ToList();
    }

    private static object MapWorkItemRow(SqlDataReader reader)
    {
        return new
        {
            workItemId = ToInt64(reader["WorkItemId"]),
            runId = reader["RunId"],
            workType = ToNullableString(reader["WorkItemType"]),
            status = ToNullableString(reader["Status"]),
            attemptCount = ToNullableInt32(reader["AttemptCount"]),
            claimedBy = ToNullableString(reader["LeaseOwner"]),
            createdAtUtc = ToNullableValue(reader["CreatedUtc"]),
            updatedAtUtc = ToNullableValue(reader["UpdatedUtc"]),
            completedAtUtc = ToNullableValue(reader["CompletedAtUtc"]),
            lastErrorMessage = ToNullableString(reader["LastErrorMessage"])
        };
    }

    private static object? GetPropertyValue(object item, string name)
    {
        var property = item.GetType().GetProperty(name);
        return property?.GetValue(item);
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("MigrationOperationalStore");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration.GetConnectionString("OperationalSql");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration["SqlOperationalRuntimeReadiness:ConnectionString"];
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration["OperationalSql:ConnectionString"];
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "MigrationOperationalStore connection string is required for runtime dashboard detail endpoints.");
        }

        return connectionString;
    }

    private static string CalculateEffectiveStatus(
        string? storedStatus,
        long total,
        long queued,
        long dispatched,
        long running,
        long completed,
        long failed)
    {
        if (total <= 0)
        {
            return string.IsNullOrWhiteSpace(storedStatus) ? "Queued" : storedStatus;
        }

        if (failed > 0 && completed + failed >= total)
        {
            return "CompletedWithFailures";
        }

        if (completed >= total)
        {
            return "Completed";
        }

        if (running > 0 || dispatched > 0 || completed > 0 || failed > 0)
        {
            return "Running";
        }

        if (queued > 0)
        {
            return "Queued";
        }

        return string.IsNullOrWhiteSpace(storedStatus) ? "Unknown" : storedStatus;
    }

    private static double CalculatePercentComplete(long total, long completed, long failed)
    {
        if (total <= 0)
        {
            return 0.0d;
        }

        return Math.Round(((double)(completed + failed) / total) * 100.0d, 2, MidpointRounding.AwayFromZero);
    }

    private static string? ToNullableString(object value)
    {
        return value is DBNull ? null : Convert.ToString(value);
    }

    private static object? ToNullableValue(object value)
    {
        return value is DBNull ? null : value;
    }

    private static long ToInt64(object value)
    {
        return value is DBNull ? 0L : Convert.ToInt64(value);
    }

    private static int? ToNullableInt32(object value)
    {
        return value is DBNull ? null : Convert.ToInt32(value);
    }
}
