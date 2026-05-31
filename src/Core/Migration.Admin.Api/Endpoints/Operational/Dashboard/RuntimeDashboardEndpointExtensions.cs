using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Migration.Admin.Api.Endpoints.Operational.Dashboard;

public static class RuntimeDashboardEndpointExtensions
{
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
    (SELECT COUNT_BIG(1) FROM migration.WorkItems WHERE Status = N'Queued') AS QueuedWorkItemCount,
    (SELECT COUNT_BIG(1) FROM migration.WorkItems WHERE Status = N'Dispatched') AS DispatchedWorkItemCount,
    (SELECT COUNT_BIG(1) FROM migration.WorkItems WHERE Status = N'Completed') AS CompletedWorkItemCount,
    (SELECT COUNT_BIG(1) FROM migration.WorkItems WHERE Status LIKE N'Failed%') AS FailedWorkItemCount;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return Results.Ok(new
            {
                runCount = 0L,
                workItemCount = 0L,
                queuedWorkItemCount = 0L,
                dispatchedWorkItemCount = 0L,
                completedWorkItemCount = 0L,
                failedWorkItemCount = 0L
            });
        }

        return Results.Ok(new
        {
            runCount = Convert.ToInt64(reader["RunCount"]),
            workItemCount = Convert.ToInt64(reader["WorkItemCount"]),
            queuedWorkItemCount = Convert.ToInt64(reader["QueuedWorkItemCount"]),
            dispatchedWorkItemCount = Convert.ToInt64(reader["DispatchedWorkItemCount"]),
            completedWorkItemCount = Convert.ToInt64(reader["CompletedWorkItemCount"]),
            failedWorkItemCount = Convert.ToInt64(reader["FailedWorkItemCount"])
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
    SUM(CASE WHEN w.Status = N'Queued' THEN 1 ELSE 0 END) AS QueuedWorkItemCount,
    SUM(CASE WHEN w.Status = N'Dispatched' THEN 1 ELSE 0 END) AS DispatchedWorkItemCount,
    SUM(CASE WHEN w.Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedWorkItemCount,
    SUM(CASE WHEN w.Status LIKE N'Failed%' THEN 1 ELSE 0 END) AS FailedWorkItemCount
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

        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new
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
                workItemCount = Convert.ToInt64(reader["WorkItemCount"]),
                queuedWorkItemCount = Convert.ToInt64(reader["QueuedWorkItemCount"]),
                dispatchedWorkItemCount = Convert.ToInt64(reader["DispatchedWorkItemCount"]),
                completedWorkItemCount = Convert.ToInt64(reader["CompletedWorkItemCount"]),
                failedWorkItemCount = Convert.ToInt64(reader["FailedWorkItemCount"])
            });
        }

        return Results.Ok(rows);
    }

    private static string ResolveConnectionString(
        IConfiguration configuration)
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
                "MigrationOperationalStore connection string is required for runtime dashboard endpoints.");
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
}
