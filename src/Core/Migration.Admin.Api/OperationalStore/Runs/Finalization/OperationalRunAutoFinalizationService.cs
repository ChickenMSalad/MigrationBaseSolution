using Migration.Infrastructure.State.OperationalStore.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunAutoFinalizationService
    : IOperationalRunAutoFinalizationService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;
    private readonly IOptions<OperationalRunAutoFinalizationOptions> _options;
    private readonly ILogger<OperationalRunAutoFinalizationService> _logger;

    public OperationalRunAutoFinalizationService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions,
        IOptions<OperationalRunAutoFinalizationOptions> options,
        ILogger<OperationalRunAutoFinalizationService> logger)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
        _options = options;
        _logger = logger;
    }

    public async Task<int> FinalizeEligibleRunsAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();
        var batchSize = Math.Clamp(_options.Value.BatchSize, 1, 500);

        var sql = $"""
            ;WITH RunRollups AS
            (
                SELECT TOP (@BatchSize)
                    r.RunId,
                    r.Status,
                    TotalWorkItemCount = COUNT(w.WorkItemId),
                    OutstandingWorkItemCount = SUM(CASE WHEN w.Status IN (N'Created', N'Locked', N'Processing') THEN 1 ELSE 0 END),
                    FailedWorkItemCount = SUM(CASE WHEN w.Status = N'Failed' THEN 1 ELSE 0 END)
                FROM [{schema}].[MigrationRuns] r
                LEFT JOIN [{schema}].[MigrationWorkItems] w
                    ON w.RunId = r.RunId
                WHERE r.Status NOT IN (N'Completed', N'Failed', N'Aborted', N'Canceled', N'CancelRequested')
                GROUP BY r.RunId, r.Status, r.CreatedAt
                HAVING COUNT(w.WorkItemId) > 0
                   AND SUM(CASE WHEN w.Status IN (N'Created', N'Locked', N'Processing') THEN 1 ELSE 0 END) = 0
                ORDER BY r.CreatedAt
            )
            UPDATE r
                SET
                    Status = CASE
                        WHEN rr.FailedWorkItemCount > 0 THEN N'Failed'
                        ELSE N'Completed'
                    END,
                    CompletedAt = CASE
                        WHEN rr.FailedWorkItemCount = 0 THEN COALESCE(r.CompletedAt, SYSDATETIMEOFFSET())
                        ELSE r.CompletedAt
                    END,
                    FailedAt = CASE
                        WHEN rr.FailedWorkItemCount > 0 THEN COALESCE(r.FailedAt, SYSDATETIMEOFFSET())
                        ELSE r.FailedAt
                    END,
                    FailureReason = CASE
                        WHEN rr.FailedWorkItemCount > 0
                        THEN COALESCE(r.FailureReason, N'Operational run auto-finalized as failed because one or more work items failed.')
                        ELSE r.FailureReason
                    END
            FROM [{schema}].[MigrationRuns] r
            INNER JOIN RunRollups rr
                ON rr.RunId = r.RunId;

            SELECT @@ROWCOUNT AS FinalizedCount;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        var finalizedCount = Convert.ToInt32(result);

        if (finalizedCount > 0)
        {
            _logger.LogInformation(
                "Operational run auto-finalization finalized {FinalizedCount} run(s).",
                finalizedCount);
        }

        return finalizedCount;
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName)
            ? "migration"
            : _sqlOptions.Value.SchemaName;
    }
}
