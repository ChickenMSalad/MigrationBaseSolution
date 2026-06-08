using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunFailureFinalizationService
    : IOperationalRunFailureFinalizationService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;
    private readonly ILogger<OperationalRunFailureFinalizationService> _logger;

    public OperationalRunFailureFinalizationService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions,
        ILogger<OperationalRunFailureFinalizationService> logger)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
        _logger = logger;
    }

    public Task<OperationalRunFailureReadinessResponse> GetReadinessAsync(
        Guid runId,
        CancellationToken cancellationToken = default) =>
        BuildReadinessAsync(runId, finalize: false, cancellationToken);

    public Task<OperationalRunFailureReadinessResponse> FinalizeAsync(
        Guid runId,
        CancellationToken cancellationToken = default) =>
        BuildReadinessAsync(runId, finalize: true, cancellationToken);

    private async Task<OperationalRunFailureReadinessResponse> BuildReadinessAsync(
        Guid runId,
        bool finalize,
        CancellationToken cancellationToken)
    {
        if (runId == Guid.Empty)
        {
            throw new ArgumentException("RunId is required.", nameof(runId));
        }

        var schema = GetSchemaName();

        var sql = $"""
            SELECT
                r.RunId,
                CurrentStatus = r.Status,
                TotalWorkItemCount = COUNT(w.WorkItemId),
                OutstandingWorkItemCount = SUM(CASE WHEN w.Status IN (N'Created', N'Locked', N'Processing') THEN 1 ELSE 0 END),
                CompletedWorkItemCount = SUM(CASE WHEN w.Status = N'Completed' THEN 1 ELSE 0 END),
                FailedWorkItemCount = SUM(CASE WHEN w.Status = N'Failed' THEN 1 ELSE 0 END),
                FailureReason = MAX(w.LastFailureReason)
            FROM [{schema}].[MigrationRuns] r
            LEFT JOIN [{schema}].[MigrationWorkItems] w
                ON w.RunId = r.RunId
            WHERE r.RunId = @RunId
            GROUP BY r.RunId, r.Status;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        string currentStatus;
        int total;
        int outstanding;
        int completed;
        int failed;
        string? failureReason;

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new OperationalRunFailureReadinessResponse
                {
                    RunId = runId,
                    CurrentStatus = "Unknown",
                    CanFinalizeFailure = false,
                    Finalized = false,
                    Message = "Operational run was not found."
                };
            }

            currentStatus = reader.GetString(reader.GetOrdinal("CurrentStatus"));
            total = ReadInt32(reader, "TotalWorkItemCount");
            outstanding = ReadInt32(reader, "OutstandingWorkItemCount");
            completed = ReadInt32(reader, "CompletedWorkItemCount");
            failed = ReadInt32(reader, "FailedWorkItemCount");
            failureReason = ReadNullableString(reader, "FailureReason");
        }

        var canFinalize = CanFinalizeFailure(
            currentStatus,
            total,
            outstanding,
            failed);

        if (!finalize || !canFinalize)
        {
            return new OperationalRunFailureReadinessResponse
            {
                RunId = runId,
                CurrentStatus = currentStatus,
                CanFinalizeFailure = canFinalize,
                Finalized = false,
                TotalWorkItemCount = total,
                OutstandingWorkItemCount = outstanding,
                CompletedWorkItemCount = completed,
                FailedWorkItemCount = failed,
                FailureReason = failureReason,
                Message = BuildMessage(currentStatus, total, outstanding, failed, canFinalize)
            };
        }

        var finalFailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? "Operational run finalized as failed because one or more work items failed."
            : failureReason;

        await FinalizeRunAsync(
            connection,
            schema,
            runId,
            finalFailureReason,
            cancellationToken);

        _logger.LogWarning(
            "Operational run {RunId} finalized as Failed. Reason: {FailureReason}",
            runId,
            finalFailureReason);

        return new OperationalRunFailureReadinessResponse
        {
            RunId = runId,
            CurrentStatus = "Failed",
            CanFinalizeFailure = true,
            Finalized = true,
            TotalWorkItemCount = total,
            OutstandingWorkItemCount = outstanding,
            CompletedWorkItemCount = completed,
            FailedWorkItemCount = failed,
            FailureReason = finalFailureReason,
            Message = "Operational run finalized as Failed."
        };
    }

    private static bool CanFinalizeFailure(
        string currentStatus,
        int total,
        int outstanding,
        int failed)
    {
        if (total == 0 || outstanding > 0 || failed == 0)
        {
            return false;
        }

        return currentStatus is not "Completed" and not "Aborted" and not "Canceled";
    }

    private static string BuildMessage(
        string currentStatus,
        int total,
        int outstanding,
        int failed,
        bool canFinalize)
    {
        return canFinalize
            ? "Operational run is ready to finalize as Failed."
            : $"Operational run cannot be finalized as Failed. Status={currentStatus}; Total={total}; Outstanding={outstanding}; Failed={failed}.";
    }

    private static async Task FinalizeRunAsync(
        SqlConnection connection,
        string schema,
        Guid runId,
        string failureReason,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            UPDATE [{schema}].[MigrationRuns]
                SET
                    Status = N'Failed',
                    FailedAt = COALESCE(FailedAt, SYSDATETIMEOFFSET()),
                    FailureReason = @FailureReason
            WHERE RunId = @RunId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@FailureReason", failureReason);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName)
            ? "migration"
            : _sqlOptions.Value.SchemaName;
    }

    private static int ReadInt32(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? 0
            : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static string? ReadNullableString(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }
}
