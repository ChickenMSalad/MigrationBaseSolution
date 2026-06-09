using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunStatusProjectionService
    : IOperationalRunStatusProjectionService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public OperationalRunStatusProjectionService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options;
    }

    public async Task<IReadOnlyCollection<OperationalRunStatusProjection>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        var sql = BuildProjectionSql(schema, whereClause: string.Empty);

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<OperationalRunStatusProjection>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(ReadProjection(reader));
        }

        return results;
    }

    public async Task<OperationalRunStatusProjection?> GetAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        var sql = BuildProjectionSql(
            schema,
            "WHERE r.RunId = @RunId");

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadProjection(reader);
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_options.Value.SchemaName)
            ? "migration"
            : _options.Value.SchemaName;
    }

    private static string BuildProjectionSql(
        string schema,
        string whereClause)
    {
        return $"""
            SELECT
                r.RunId,
                r.SourceSystem,
                r.TargetSystem,
                RunStatus = r.Status,
                r.CreatedAt,
                r.StartedAt,
                r.CompletedAt,
                r.FailedAt,
                r.FailureReason,

                ManifestRecordCount = COUNT(DISTINCT m.ManifestRecordId),
                ManifestCreatedCount = COUNT(DISTINCT CASE WHEN m.Status = 'Created' THEN m.ManifestRecordId END),
                ManifestProcessingCount = COUNT(DISTINCT CASE WHEN m.Status = 'Processing' THEN m.ManifestRecordId END),
                ManifestCompletedCount = COUNT(DISTINCT CASE WHEN m.Status = 'Completed' THEN m.ManifestRecordId END),
                ManifestFailedCount = COUNT(DISTINCT CASE WHEN m.Status = 'Failed' THEN m.ManifestRecordId END),

                WorkItemCount = COUNT(DISTINCT w.WorkItemId),
                WorkItemCreatedCount = COUNT(DISTINCT CASE WHEN w.Status = 'Created' THEN w.WorkItemId END),
                WorkItemLockedCount = COUNT(DISTINCT CASE WHEN w.Status = 'Locked' THEN w.WorkItemId END),
                WorkItemProcessingCount = COUNT(DISTINCT CASE WHEN w.Status = 'Processing' THEN w.WorkItemId END),
                WorkItemCompletedCount = COUNT(DISTINCT CASE WHEN w.Status = 'Completed' THEN w.WorkItemId END),
                WorkItemFailedCount = COUNT(DISTINCT CASE WHEN w.Status = 'Failed' THEN w.WorkItemId END),

                FailureCount = COUNT(DISTINCT f.FailureId),
                CheckpointCount = COUNT(DISTINCT c.CheckpointId)
            FROM [{schema}].[Runs] r
            LEFT JOIN [{schema}].[MigrationManifestRecords] m
                ON m.RunId = r.RunId
            LEFT JOIN [{schema}].[WorkItems] w
                ON w.RunId = r.RunId
            LEFT JOIN [{schema}].[MigrationFailures] f
                ON f.RunId = r.RunId
            LEFT JOIN [{schema}].[MigrationCheckpoints] c
                ON c.RunId = r.RunId
            {whereClause}
            GROUP BY
                r.RunId,
                r.SourceSystem,
                r.TargetSystem,
                r.Status,
                r.CreatedAt,
                r.StartedAt,
                r.CompletedAt,
                r.FailedAt,
                r.FailureReason
            ORDER BY r.CreatedAt DESC;
            """;
    }

    private static OperationalRunStatusProjection ReadProjection(
        SqlDataReader reader)
    {
        var workItemCount = reader.GetInt32(reader.GetOrdinal("WorkItemCount"));
        var workItemCompletedCount = reader.GetInt32(reader.GetOrdinal("WorkItemCompletedCount"));
        var workItemFailedCount = reader.GetInt32(reader.GetOrdinal("WorkItemFailedCount"));
        var failureCount = reader.GetInt32(reader.GetOrdinal("FailureCount"));

        var completionPercent = workItemCount == 0
            ? 0m
            : decimal.Round(
                ((decimal)(workItemCompletedCount + workItemFailedCount) / workItemCount) * 100m,
                2);

        return new OperationalRunStatusProjection
        {
            RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
            SourceSystem = reader.GetString(reader.GetOrdinal("SourceSystem")),
            TargetSystem = reader.GetString(reader.GetOrdinal("TargetSystem")),
            RunStatus = reader.GetString(reader.GetOrdinal("RunStatus")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
            StartedAt = ReadNullableDateTimeOffset(reader, "StartedAt"),
            CompletedAt = ReadNullableDateTimeOffset(reader, "CompletedAt"),
            FailedAt = ReadNullableDateTimeOffset(reader, "FailedAt"),
            FailureReason = ReadNullableString(reader, "FailureReason"),

            ManifestRecordCount = reader.GetInt32(reader.GetOrdinal("ManifestRecordCount")),
            ManifestCreatedCount = reader.GetInt32(reader.GetOrdinal("ManifestCreatedCount")),
            ManifestProcessingCount = reader.GetInt32(reader.GetOrdinal("ManifestProcessingCount")),
            ManifestCompletedCount = reader.GetInt32(reader.GetOrdinal("ManifestCompletedCount")),
            ManifestFailedCount = reader.GetInt32(reader.GetOrdinal("ManifestFailedCount")),

            WorkItemCount = workItemCount,
            WorkItemCreatedCount = reader.GetInt32(reader.GetOrdinal("WorkItemCreatedCount")),
            WorkItemLockedCount = reader.GetInt32(reader.GetOrdinal("WorkItemLockedCount")),
            WorkItemProcessingCount = reader.GetInt32(reader.GetOrdinal("WorkItemProcessingCount")),
            WorkItemCompletedCount = workItemCompletedCount,
            WorkItemFailedCount = workItemFailedCount,

            FailureCount = failureCount,
            CheckpointCount = reader.GetInt32(reader.GetOrdinal("CheckpointCount")),
            CompletionPercent = completionPercent,
            ProjectionStatus = DetermineProjectionStatus(
                reader.GetString(reader.GetOrdinal("RunStatus")),
                workItemCount,
                workItemCompletedCount,
                workItemFailedCount,
                failureCount)
        };
    }

    private static string DetermineProjectionStatus(
        string runStatus,
        int workItemCount,
        int completedCount,
        int failedCount,
        int failureCount)
    {
        if (failedCount > 0 || failureCount > 0)
        {
            return "FailedOrPartiallyFailed";
        }

        if (workItemCount > 0 && completedCount == workItemCount)
        {
            return "Completed";
        }

        if (workItemCount > 0)
        {
            return "InProgressOrPending";
        }

        return runStatus;
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

    private static DateTimeOffset? ReadNullableDateTimeOffset(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }
}


