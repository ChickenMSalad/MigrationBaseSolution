using System.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Migration.Admin.Api.Endpoints.Operational.Dashboard;

public static class RuntimeDashboardDetailEndpointExtensions
{
    private const int DefaultRetryLimit = 3;

    public static IEndpointRouteBuilder MapSqlOperationalRuntimeDashboardDetailEndpoints(
        this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app
            .MapGroup("/api/runtime/dashboard")
            .WithTags("Runtime Dashboard");

        group.MapGet("/runs/{runId:guid}", GetRunDetailAsync)
            .WithName("GetRuntimeDashboardRunDetail");

        group.MapGet("/runs/{runId:guid}/work-items", GetRunWorkItemsAsync)
            .WithName("GetRuntimeDashboardRunWorkItems");

        group.MapGet("/runs/{runId:guid}/failures", GetRunFailuresAsync)
            .WithName("GetRuntimeDashboardRunFailures");

        group.MapGet("/runs/{runId:guid}/progress", GetRunProgressAsync)
            .WithName("GetRuntimeDashboardRunProgress");

        return app;
    }

    private static async Task<IResult> GetRunDetailAsync(
        Guid runId,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString(configuration);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

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
    SUM(CASE WHEN w.Status = N'Queued' THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS QueuedWorkItemCount,
    SUM(CASE WHEN w.Status = N'Dispatched' THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS DispatchedWorkItemCount,
    SUM(CASE WHEN w.Status IN (N'Running', N'InProgress', N'Processing', N'Started', N'Executing') OR (w.StartedAtUtc IS NOT NULL AND w.CompletedAtUtc IS NULL) THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS RunningWorkItemCount,
    SUM(CASE WHEN w.Status = N'Completed' THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS CompletedWorkItemCount,
    SUM(CASE WHEN w.Status LIKE N'Failed%' OR w.LastErrorMessage IS NOT NULL THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS FailedWorkItemCount,
    SUM(CASE WHEN (w.Status LIKE N'Failed%' OR w.LastErrorMessage IS NOT NULL) AND ISNULL(w.AttemptCount, 0) < @RetryLimit THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS RetryableWorkItemCount,
    MIN(w.StartedAtUtc) AS FirstWorkItemStartedAtUtc,
    MAX(w.CompletedAtUtc) AS LastWorkItemCompletedAtUtc
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
        command.Parameters.Add(new SqlParameter("@RetryLimit", SqlDbType.Int) { Value = DefaultRetryLimit });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return Results.NotFound(new { runId });
        }

        var total = ToInt64(reader, "WorkItemCount");
        var queued = ToInt64(reader, "QueuedWorkItemCount");
        var dispatched = ToInt64(reader, "DispatchedWorkItemCount");
        var running = ToInt64(reader, "RunningWorkItemCount");
        var completed = ToInt64(reader, "CompletedWorkItemCount");
        var failed = ToInt64(reader, "FailedWorkItemCount");
        var status = ToNullableString(reader, "Status");

        return Results.Ok(new
        {
            runId = reader["RunId"],
            runKey = ToNullableString(reader, "RunKey"),
            runName = ToNullableString(reader, "RunName"),
            sourceSystem = ToNullableString(reader, "SourceSystem"),
            targetSystem = ToNullableString(reader, "TargetSystem"),
            status,
            effectiveStatus = CalculateEffectiveStatus(status, total, queued, dispatched, running, completed, failed),
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

    private static Task<IResult> GetRunWorkItemsAsync(
        Guid runId,
        IConfiguration configuration,
        int? take,
        CancellationToken cancellationToken)
    {
        return GetRunWorkItemRowsAsync(
            runId,
            configuration,
            take,
            failedOnly: false,
            cancellationToken);
    }

    private static Task<IResult> GetRunFailuresAsync(
        Guid runId,
        IConfiguration configuration,
        int? take,
        CancellationToken cancellationToken)
    {
        return GetRunWorkItemRowsAsync(
            runId,
            configuration,
            take,
            failedOnly: true,
            cancellationToken);
    }

    private static async Task<IResult> GetRunProgressAsync(
        Guid runId,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        return await GetRunDetailAsync(runId, configuration, cancellationToken);
    }

    private static async Task<IResult> GetRunWorkItemRowsAsync(
        Guid runId,
        IConfiguration configuration,
        int? take,
        bool failedOnly,
        CancellationToken cancellationToken)
    {
        var rowLimit = Math.Clamp(take.GetValueOrDefault(100), 1, 500);
        var connectionString = ResolveConnectionString(configuration);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = failedOnly
            ? @"
SELECT TOP (@Take)
    WorkItemId,
    RunId,
    WorkType,
    Status,
    AttemptCount,
    ClaimedBy,
    StartedAtUtc,
    CreatedAtUtc,
    UpdatedAtUtc,
    CompletedAtUtc,
    LastErrorMessage,
    ResultJson
FROM migration.WorkItems
WHERE RunId = @RunId
  AND (Status LIKE N'Failed%' OR LastErrorMessage IS NOT NULL)
ORDER BY COALESCE(UpdatedAtUtc, CreatedAtUtc) DESC, WorkItemId DESC;"
            : @"
SELECT TOP (@Take)
    WorkItemId,
    RunId,
    WorkType,
    Status,
    AttemptCount,
    ClaimedBy,
    StartedAtUtc,
    CreatedAtUtc,
    UpdatedAtUtc,
    CompletedAtUtc,
    LastErrorMessage,
    ResultJson
FROM migration.WorkItems
WHERE RunId = @RunId
ORDER BY COALESCE(UpdatedAtUtc, CreatedAtUtc) DESC, WorkItemId DESC;";
        command.Parameters.Add(new SqlParameter("@RunId", SqlDbType.UniqueIdentifier) { Value = runId });
        command.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = rowLimit });

        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var attemptCount = ToInt32(reader, "AttemptCount");
            var status = ToNullableString(reader, "Status");
            var errorMessage = ToNullableString(reader, "LastErrorMessage");
            rows.Add(new
            {
                workItemId = ToInt64(reader, "WorkItemId"),
                runId = reader["RunId"],
                workType = ToNullableString(reader, "WorkType"),
                status,
                attemptCount,
                isRunning = IsRunningWorkItem(status, ToNullableValue(reader, "StartedAtUtc"), ToNullableValue(reader, "CompletedAtUtc")),
                isRetryable = IsRetryable(status, errorMessage, attemptCount),
                claimedBy = ToNullableString(reader, "ClaimedBy"),
                startedAtUtc = ToNullableValue(reader, "StartedAtUtc"),
                createdAtUtc = ToNullableValue(reader, "CreatedAtUtc"),
                updatedAtUtc = ToNullableValue(reader, "UpdatedAtUtc"),
                completedAtUtc = ToNullableValue(reader, "CompletedAtUtc"),
                lastErrorMessage = errorMessage,
                resultJson = ToNullableString(reader, "ResultJson")
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

    private static bool IsRunningWorkItem(string? status, object? startedAtUtc, object? completedAtUtc)
    {
        if (string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "InProgress", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Processing", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Started", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Executing", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return startedAtUtc is not null && completedAtUtc is null;
    }

    private static bool IsRetryable(string? status, string? errorMessage, int attemptCount)
    {
        var failed = !string.IsNullOrWhiteSpace(errorMessage)
            || (!string.IsNullOrWhiteSpace(status) && status.StartsWith("Failed", StringComparison.OrdinalIgnoreCase));

        return failed && attemptCount < DefaultRetryLimit;
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

    private static int ToInt32(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static bool ToBoolean(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal));
    }
}
