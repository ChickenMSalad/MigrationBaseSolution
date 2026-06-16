using System.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Migration.Admin.Api.Endpoints.Operational.Dashboard;

public static class RuntimeDashboardEndpointExtensions
{
    private const int DefaultRetryLimit = 3;

    public static IEndpointRouteBuilder MapSqlOperationalRuntimeDashboardEndpoints(
        this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app
            .MapGroup("/api/runtime/dashboard")
            .WithTags("Runtime Dashboard");

        group.MapGet("/summary", GetSummaryAsync)
            .WithName("GetRuntimeDashboardSummary");

        group.MapGet("/runs", GetRunsAsync)
            .WithName("GetRuntimeDashboardRuns");

        return app;
    }

    private static async Task<IResult> GetSummaryAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString(configuration);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = @"
SELECT
    (SELECT COUNT_BIG(1) FROM migration.Runs) AS RunCount,
    (SELECT COUNT_BIG(1) FROM migration.WorkItems) AS WorkItemCount,
    (SELECT COUNT_BIG(1) FROM migration.WorkItems WHERE Status IN (N'Pending', N'Ready', N'Queued', N'FailedRetryable')) AS QueuedWorkItemCount,
    (SELECT COUNT_BIG(1) FROM migration.WorkItems WHERE Status IN (N'Dispatching', N'Dispatched')) AS DispatchedWorkItemCount,
    (SELECT COUNT_BIG(1) FROM migration.WorkItems WHERE Status IN (N'Leased', N'Running', N'InProgress', N'Processing', N'Started', N'Executing') OR (StartedAtUtc IS NOT NULL AND CompletedAtUtc IS NULL AND Status NOT IN (N'Completed'))) AS RunningWorkItemCount,
    (SELECT COUNT_BIG(1) FROM migration.WorkItems WHERE Status = N'Completed') AS CompletedWorkItemCount,
    (SELECT COUNT_BIG(1) FROM migration.WorkItems WHERE Status LIKE N'Failed%' OR LastErrorMessage IS NOT NULL) AS FailedWorkItemCount,
    (SELECT COUNT_BIG(1) FROM migration.WorkItems WHERE (Status LIKE N'Failed%' OR LastErrorMessage IS NOT NULL) AND ISNULL(AttemptCount, 0) < @RetryLimit) AS RetryableWorkItemCount;";
        command.Parameters.Add(new SqlParameter("@RetryLimit", SqlDbType.Int) { Value = DefaultRetryLimit });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return Results.Ok(new
            {
                runCount = 0L,
                workItemCount = 0L,
                queuedWorkItemCount = 0L,
                dispatchedWorkItemCount = 0L,
                runningWorkItemCount = 0L,
                completedWorkItemCount = 0L,
                failedWorkItemCount = 0L,
                retryableWorkItemCount = 0L,
                percentComplete = 0.0d,
                activeWorkItemCount = 0L
            });
        }

        var total = ToInt64(reader, "WorkItemCount");
        var queued = ToInt64(reader, "QueuedWorkItemCount");
        var dispatched = ToInt64(reader, "DispatchedWorkItemCount");
        var running = ToInt64(reader, "RunningWorkItemCount");
        var completed = ToInt64(reader, "CompletedWorkItemCount");
        var failed = ToInt64(reader, "FailedWorkItemCount");

        return Results.Ok(new
        {
            runCount = ToInt64(reader, "RunCount"),
            workItemCount = total,
            queuedWorkItemCount = queued,
            dispatchedWorkItemCount = dispatched,
            runningWorkItemCount = running,
            completedWorkItemCount = completed,
            failedWorkItemCount = failed,
            retryableWorkItemCount = ToInt64(reader, "RetryableWorkItemCount"),
            percentComplete = CalculatePercentComplete(total, completed, failed),
            activeWorkItemCount = queued + dispatched + running
        });
    }

    private static async Task<IResult> GetRunsAsync(
        IConfiguration configuration,
        int? take,
        CancellationToken cancellationToken)
    {
        var rowLimit = Math.Clamp(take.GetValueOrDefault(50), 1, 200);
        var connectionString = ResolveConnectionString(configuration);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = @"
SELECT TOP (@Take)
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
    SUM(CASE WHEN (w.Status LIKE N'Failed%' OR w.LastErrorMessage IS NOT NULL) AND ISNULL(w.AttemptCount, 0) < @RetryLimit THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS RetryableWorkItemCount,
    MIN(w.StartedAtUtc) AS FirstWorkItemStartedAtUtc,
    MAX(w.CompletedAtUtc) AS LastWorkItemCompletedAtUtc
FROM migration.Runs r
LEFT JOIN migration.WorkItems w ON w.RunId = r.RunId
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
    r.UpdatedAtUtc
ORDER BY COALESCE(r.UpdatedAtUtc, r.CreatedAtUtc, r.RequestedAtUtc) DESC;";
        command.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = rowLimit });
        command.Parameters.Add(new SqlParameter("@RetryLimit", SqlDbType.Int) { Value = DefaultRetryLimit });

        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var total = ToInt64(reader, "WorkItemCount");
            var queued = ToInt64(reader, "QueuedWorkItemCount");
            var dispatched = ToInt64(reader, "DispatchedWorkItemCount");
            var running = ToInt64(reader, "RunningWorkItemCount");
            var completed = ToInt64(reader, "CompletedWorkItemCount");
            var failed = ToInt64(reader, "FailedWorkItemCount");
            var status = ToNullableString(reader, "Status");
            var effectiveStatus = CalculateEffectiveStatus(status, total, queued, dispatched, running, completed, failed);

            rows.Add(new
            {
                runId = reader["RunId"],
                runKey = ToNullableString(reader, "RunKey"),
                runName = ToNullableString(reader, "RunName"),
                sourceSystem = ToNullableString(reader, "SourceSystem"),
                targetSystem = ToNullableString(reader, "TargetSystem"),
                status,
                effectiveStatus,
                environmentName = ToNullableString(reader, "EnvironmentName"),
                isDryRun = ToBoolean(reader, "IsDryRun"),
                requestedAtUtc = ToNullableValue(reader, "RequestedAtUtc"),
                createdAtUtc = ToNullableValue(reader, "CreatedAtUtc"),
                updatedAtUtc = ToNullableValue(reader, "UpdatedAtUtc"),
                firstWorkItemStartedAtUtc = ToNullableValue(reader, "FirstWorkItemStartedAtUtc"),
                lastWorkItemCompletedAtUtc = ToNullableValue(reader, "LastWorkItemCompletedAtUtc"),
                workItemCount = total,
                queuedWorkItemCount = queued,
                dispatchedWorkItemCount = dispatched,
                runningWorkItemCount = running,
                completedWorkItemCount = completed,
                failedWorkItemCount = failed,
                retryableWorkItemCount = ToInt64(reader, "RetryableWorkItemCount"),
                activeWorkItemCount = queued + dispatched + running,
                percentComplete = CalculatePercentComplete(total, completed, failed)
            });
        }

        return Results.Ok(rows);
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
                "MigrationOperationalStore connection string is required for runtime dashboard endpoints.");
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

    private static string? ToNullableString(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal));
    }

    private static object? ToNullableValue(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
    }

    private static long ToInt64(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0L : Convert.ToInt64(reader.GetValue(ordinal));
    }

    private static bool ToBoolean(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal));
    }
}
