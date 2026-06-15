using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

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

        group.MapGet("/runs/{runId:guid}", GetRunDetailAsync)
            .WithName("GetRuntimeDashboardRunDetail");

        group.MapGet("/runs/{runId:guid}/work-items", GetRunWorkItemsAsync)
            .WithName("GetRuntimeDashboardRunWorkItems");

        group.MapGet("/runs/{runId:guid}/failures", GetRunFailuresAsync)
            .WithName("GetRuntimeDashboardRunFailures");

        group.MapGet("/failures", GetAllFailuresAsync)
            .WithName("GetRuntimeDashboardFailures");

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
    SUM(CASE WHEN w.Status IN (N'Pending', N'Queued') THEN 1 ELSE 0 END) AS QueuedWorkItemCount,
    SUM(CASE WHEN w.Status = N'Dispatched' THEN 1 ELSE 0 END) AS DispatchedWorkItemCount,
    SUM(CASE WHEN w.Status IN (N'Running', N'InProgress', N'Processing') THEN 1 ELSE 0 END) AS RunningWorkItemCount,
    SUM(CASE WHEN w.Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedWorkItemCount,
    SUM(CASE WHEN w.Status LIKE N'Failed%' THEN 1 ELSE 0 END) AS FailedWorkItemCount,
    SUM(CASE WHEN w.Status IN (N'Retryable', N'FailedRetryable') THEN 1 ELSE 0 END) AS RetryableWorkItemCount,
    SUM(CASE WHEN w.Status = N'RetryQueued' THEN 1 ELSE 0 END) AS RetryQueuedWorkItemCount
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

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return Results.NotFound(new { runId });
        }

        var total = ToInt64(reader["WorkItemCount"]);
        var completed = ToInt64(reader["CompletedWorkItemCount"]);
        var failed = ToInt64(reader["FailedWorkItemCount"]);
        var terminal = completed + failed;

        return Results.Ok(new
        {
            runId = reader["RunId"],
            runKey = ToNullableString(reader["RunKey"]),
            runName = ToNullableString(reader["RunName"]),
            sourceSystem = ToNullableString(reader["SourceSystem"]),
            targetSystem = ToNullableString(reader["TargetSystem"]),
            status = ToNullableString(reader["Status"]),
            environmentName = ToNullableString(reader["EnvironmentName"]),
            isDryRun = reader["IsDryRun"] is not DBNull && Convert.ToBoolean(reader["IsDryRun"]),
            requestedAtUtc = ToNullableValue(reader["RequestedAtUtc"]),
            createdAtUtc = ToNullableValue(reader["CreatedAtUtc"]),
            updatedAtUtc = ToNullableValue(reader["UpdatedAtUtc"]),
            workItemCount = total,
            queuedWorkItemCount = ToInt64(reader["QueuedWorkItemCount"]),
            dispatchedWorkItemCount = ToInt64(reader["DispatchedWorkItemCount"]),
            runningWorkItemCount = ToInt64(reader["RunningWorkItemCount"]),
            completedWorkItemCount = completed,
            failedWorkItemCount = failed,
            retryableWorkItemCount = ToInt64(reader["RetryableWorkItemCount"]),
            retryQueuedWorkItemCount = ToInt64(reader["RetryQueuedWorkItemCount"]),
            percentComplete = total == 0 ? 0 : Math.Round((decimal)terminal * 100m / total, 2)
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

    private static async Task<IResult> GetAllFailuresAsync(
        IConfiguration configuration,
        int? take,
        CancellationToken cancellationToken)
    {
        var rowLimit = Math.Clamp(take.GetValueOrDefault(50), 1, 500);
        var connectionString = ResolveConnectionString(configuration);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var summary = await ReadFailureSummaryAsync(connection, cancellationToken);
        var rows = await ReadFailureRowsAsync(connection, rowLimit, cancellationToken);

        return Results.Ok(new
        {
            summary,
            workItems = rows,
            message = rows.Count == 0
                ? "No failed or retryable work items were found in migration.WorkItems."
                : null
        });
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

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
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
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(MapWorkItemRow(reader));
        }

        return rows;
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
        command.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = rowLimit });

        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(MapWorkItemRow(reader));
        }

        return Results.Ok(rows);
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

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("MigrationOperationalStore");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration["SqlOperationalRuntimeReadiness:ConnectionString"];
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "MigrationOperationalStore connection string is required for runtime dashboard detail endpoints.");
        }

        return connectionString;
    }

    private static string? ToNullableString(object value)
    {
        return value is DBNull ? null : Convert.ToString(value);
    }

    private static object? ToNullableValue(object value)
    {
        return value is DBNull ? null : value;
    }

    private static int? ToNullableInt32(object value)
    {
        return value is DBNull ? null : Convert.ToInt32(value);
    }

    private static long ToInt64(object value)
    {
        return value is DBNull ? 0L : Convert.ToInt64(value);
    }
}
