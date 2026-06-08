using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunStatusReconciliationService : IOperationalRunStatusReconciliationService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;
    private readonly ILogger<OperationalRunStatusReconciliationService> _logger;

    public OperationalRunStatusReconciliationService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions,
        ILogger<OperationalRunStatusReconciliationService> logger)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
        _logger = logger;
    }

    public Task<OperationalRunStatusReconciliationResponse> PreviewAsync(Guid runId, CancellationToken cancellationToken = default) =>
        BuildResponseAsync(runId, apply: false, cancellationToken);

    public Task<OperationalRunStatusReconciliationResponse> ApplyAsync(Guid runId, CancellationToken cancellationToken = default) =>
        BuildResponseAsync(runId, apply: true, cancellationToken);

    private async Task<OperationalRunStatusReconciliationResponse> BuildResponseAsync(Guid runId, bool apply, CancellationToken cancellationToken)
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
                CreatedWorkItemCount = SUM(CASE WHEN w.Status = N'Created' THEN 1 ELSE 0 END),
                LockedWorkItemCount = SUM(CASE WHEN w.Status = N'Locked' THEN 1 ELSE 0 END),
                ProcessingWorkItemCount = SUM(CASE WHEN w.Status = N'Processing' THEN 1 ELSE 0 END),
                CompletedWorkItemCount = SUM(CASE WHEN w.Status = N'Completed' THEN 1 ELSE 0 END),
                FailedWorkItemCount = SUM(CASE WHEN w.Status = N'Failed' THEN 1 ELSE 0 END)
            FROM [{schema}].[MigrationRuns] r
            LEFT JOIN [{schema}].[MigrationWorkItems] w
                ON w.RunId = r.RunId
            WHERE r.RunId = @RunId
            GROUP BY r.RunId, r.Status;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        string currentStatus;
        int total;
        int created;
        int locked;
        int processing;
        int completed;
        int failed;

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new OperationalRunStatusReconciliationResponse
                {
                    RunId = runId,
                    CurrentStatus = "Unknown",
                    RecommendedStatus = "Unknown",
                    Reason = "Operational run was not found."
                };
            }

            currentStatus = reader.GetString(reader.GetOrdinal("CurrentStatus"));
            total = ReadInt32(reader, "TotalWorkItemCount");
            created = ReadInt32(reader, "CreatedWorkItemCount");
            locked = ReadInt32(reader, "LockedWorkItemCount");
            processing = ReadInt32(reader, "ProcessingWorkItemCount");
            completed = ReadInt32(reader, "CompletedWorkItemCount");
            failed = ReadInt32(reader, "FailedWorkItemCount");
        }

        var outstanding = created + locked + processing;
        var recommended = DetermineRecommendedStatus(currentStatus, total, outstanding, completed, failed);
        var wouldChange = !string.Equals(currentStatus, recommended, StringComparison.OrdinalIgnoreCase);
        var applied = false;

        if (apply && wouldChange && recommended != "Unknown")
        {
            await ApplyStatusAsync(connection, schema, runId, recommended, cancellationToken);
            applied = true;
            _logger.LogInformation("Operational run {RunId} status reconciled from {CurrentStatus} to {RecommendedStatus}.", runId, currentStatus, recommended);
        }

        return new OperationalRunStatusReconciliationResponse
        {
            RunId = runId,
            CurrentStatus = currentStatus,
            RecommendedStatus = recommended,
            WouldChange = wouldChange,
            Applied = applied,
            TotalWorkItemCount = total,
            CreatedWorkItemCount = created,
            LockedWorkItemCount = locked,
            ProcessingWorkItemCount = processing,
            CompletedWorkItemCount = completed,
            FailedWorkItemCount = failed,
            OutstandingWorkItemCount = outstanding,
            Reason = $"CurrentStatus={currentStatus}; RecommendedStatus={recommended}; Total={total}; Outstanding={outstanding}; Completed={completed}; Failed={failed}."
        };
    }

    private static string DetermineRecommendedStatus(string currentStatus, int total, int outstanding, int completed, int failed)
    {
        if (string.Equals(currentStatus, "Aborted", StringComparison.OrdinalIgnoreCase)) return "Aborted";
        if (string.Equals(currentStatus, "CancelRequested", StringComparison.OrdinalIgnoreCase)) return outstanding == 0 ? "Canceled" : "CancelRequested";
        if (total == 0) return "Created";
        if (failed > 0 && outstanding == 0) return "Failed";
        if (completed == total) return "Completed";
        if (outstanding > 0 && completed + failed > 0) return "Started";
        if (outstanding > 0) return "Created";
        return currentStatus;
    }

    private static async Task ApplyStatusAsync(SqlConnection connection, string schema, Guid runId, string status, CancellationToken cancellationToken)
    {
        var sql = $"""
            UPDATE [{schema}].[MigrationRuns]
                SET
                    Status = @Status,
                    CompletedAt = CASE
                        WHEN @Status IN (N'Completed', N'Canceled', N'Failed', N'Aborted')
                        THEN COALESCE(CompletedAt, SYSDATETIMEOFFSET())
                        ELSE CompletedAt
                    END,
                    FailedAt = CASE
                        WHEN @Status = N'Failed'
                        THEN COALESCE(FailedAt, SYSDATETIMEOFFSET())
                        ELSE FailedAt
                    END
            WHERE RunId = @RunId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@Status", status);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string GetSchemaName() => string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName) ? "migration" : _sqlOptions.Value.SchemaName;

    private static int ReadInt32(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }
}
