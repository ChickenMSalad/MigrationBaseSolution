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
    SUM(CASE WHEN w.Status = N'Queued' THEN 1 ELSE 0 END) AS QueuedWorkItemCount,
    SUM(CASE WHEN w.Status = N'Dispatched' THEN 1 ELSE 0 END) AS DispatchedWorkItemCount,
    SUM(CASE WHEN w.Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedWorkItemCount,
    SUM(CASE WHEN w.Status LIKE N'Failed%' THEN 1 ELSE 0 END) AS FailedWorkItemCount
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
            workItemCount = Convert.ToInt64(reader["WorkItemCount"]),
            queuedWorkItemCount = Convert.ToInt64(reader["QueuedWorkItemCount"]),
            dispatchedWorkItemCount = Convert.ToInt64(reader["DispatchedWorkItemCount"]),
            completedWorkItemCount = Convert.ToInt64(reader["CompletedWorkItemCount"]),
            failedWorkItemCount = Convert.ToInt64(reader["FailedWorkItemCount"])
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
    CreatedAtUtc,
    UpdatedAtUtc,
    CompletedAtUtc,
    LastErrorMessage
FROM migration.WorkItems
WHERE RunId = @RunId
  AND (Status LIKE N'Failed%' OR LastErrorMessage IS NOT NULL)
ORDER BY COALESCE(UpdatedAtUtc, CreatedAtUtc) DESC,
         WorkItemId DESC;"
            : @"
SELECT TOP (@Take)
    WorkItemId,
    RunId,
    WorkType,
    Status,
    AttemptCount,
    ClaimedBy,
    CreatedAtUtc,
    UpdatedAtUtc,
    CompletedAtUtc,
    LastErrorMessage
FROM migration.WorkItems
WHERE RunId = @RunId
ORDER BY COALESCE(UpdatedAtUtc, CreatedAtUtc) DESC,
         WorkItemId DESC;";
        command.Parameters.Add(new SqlParameter("@RunId", SqlDbType.UniqueIdentifier) { Value = runId });
        command.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = rowLimit });

        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new
            {
                workItemId = Convert.ToInt64(reader["WorkItemId"]),
                runId = reader["RunId"],
                workType = ToNullableString(reader["WorkType"]),
                status = ToNullableString(reader["Status"]),
                attemptCount = Convert.ToInt32(reader["AttemptCount"]),
                claimedBy = ToNullableString(reader["ClaimedBy"]),
                createdAtUtc = ToNullableValue(reader["CreatedAtUtc"]),
                updatedAtUtc = ToNullableValue(reader["UpdatedAtUtc"]),
                completedAtUtc = ToNullableValue(reader["CompletedAtUtc"]),
                lastErrorMessage = ToNullableString(reader["LastErrorMessage"])
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
}


