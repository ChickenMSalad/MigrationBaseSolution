using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalActivityFeedService
    : IOperationalGlobalActivityFeedService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public OperationalGlobalActivityFeedService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options;
    }

    public async Task<OperationalGlobalActivityFeedResponse> GetRecentActivityAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();
        var safeLimit = Math.Clamp(limit, 1, 500);

        var sql = $"""
            SELECT TOP (@Limit)
                RunId,
                OccurredAt,
                EventType,
                Source,
                WorkItemId,
                ManifestRecordId,
                CheckpointId,
                FailureId,
                Message
            FROM
            (
                SELECT
                    r.RunId,
                    OccurredAt = r.CreatedAt,
                    EventType = CAST(N'RunCreated' AS NVARCHAR(200)),
                    Source = CAST(N'Runs' AS NVARCHAR(200)),
                    WorkItemId = CAST(NULL AS UNIQUEIDENTIFIER),
                    ManifestRecordId = CAST(NULL AS UNIQUEIDENTIFIER),
                    CheckpointId = CAST(NULL AS UNIQUEIDENTIFIER),
                    FailureId = CAST(NULL AS UNIQUEIDENTIFIER),
                    Message = CAST(N'Operational run created with status ' + r.Status + N'.' AS NVARCHAR(4000))
                FROM [{schema}].[Runs] r

                UNION ALL

                SELECT
                    r.RunId,
                    OccurredAt = r.CompletedAt,
                    EventType = CAST(N'RunCompleted' AS NVARCHAR(200)),
                    Source = CAST(N'Runs' AS NVARCHAR(200)),
                    WorkItemId = CAST(NULL AS UNIQUEIDENTIFIER),
                    ManifestRecordId = CAST(NULL AS UNIQUEIDENTIFIER),
                    CheckpointId = CAST(NULL AS UNIQUEIDENTIFIER),
                    FailureId = CAST(NULL AS UNIQUEIDENTIFIER),
                    Message = CAST(N'Operational run completed.' AS NVARCHAR(4000))
                FROM [{schema}].[Runs] r
                WHERE r.CompletedAt IS NOT NULL

                UNION ALL

                SELECT
                    r.RunId,
                    OccurredAt = r.FailedAt,
                    EventType = CAST(N'RunFailed' AS NVARCHAR(200)),
                    Source = CAST(N'Runs' AS NVARCHAR(200)),
                    WorkItemId = CAST(NULL AS UNIQUEIDENTIFIER),
                    ManifestRecordId = CAST(NULL AS UNIQUEIDENTIFIER),
                    CheckpointId = CAST(NULL AS UNIQUEIDENTIFIER),
                    FailureId = CAST(NULL AS UNIQUEIDENTIFIER),
                    Message = CAST(COALESCE(r.FailureReason, N'Operational run failed.') AS NVARCHAR(4000))
                FROM [{schema}].[Runs] r
                WHERE r.FailedAt IS NOT NULL

                UNION ALL

                SELECT
                    w.RunId,
                    OccurredAt = w.CreatedAt,
                    EventType = CAST(N'WorkItemCreated' AS NVARCHAR(200)),
                    Source = CAST(N'WorkItems' AS NVARCHAR(200)),
                    WorkItemId = w.WorkItemId,
                    ManifestRecordId = w.ManifestRecordId,
                    CheckpointId = CAST(NULL AS UNIQUEIDENTIFIER),
                    FailureId = CAST(NULL AS UNIQUEIDENTIFIER),
                    Message = CAST(N'Work item created with status ' + w.Status + N'.' AS NVARCHAR(4000))
                FROM [{schema}].[WorkItems] w

                UNION ALL

                SELECT
                    w.RunId,
                    OccurredAt = w.LockedAt,
                    EventType = CAST(N'WorkItemLocked' AS NVARCHAR(200)),
                    Source = CAST(N'WorkItems' AS NVARCHAR(200)),
                    WorkItemId = w.WorkItemId,
                    ManifestRecordId = w.ManifestRecordId,
                    CheckpointId = CAST(NULL AS UNIQUEIDENTIFIER),
                    FailureId = CAST(NULL AS UNIQUEIDENTIFIER),
                    Message = CAST(CASE
                        WHEN w.LockedBy IS NULL THEN N'Work item locked.'
                        ELSE N'Work item locked by ' + w.LockedBy + N'.'
                    END AS NVARCHAR(4000))
                FROM [{schema}].[WorkItems] w
                WHERE w.LockedAt IS NOT NULL

                UNION ALL

                SELECT
                    w.RunId,
                    OccurredAt = w.CompletedAt,
                    EventType = CAST(N'WorkItemCompleted' AS NVARCHAR(200)),
                    Source = CAST(N'WorkItems' AS NVARCHAR(200)),
                    WorkItemId = w.WorkItemId,
                    ManifestRecordId = w.ManifestRecordId,
                    CheckpointId = CAST(NULL AS UNIQUEIDENTIFIER),
                    FailureId = CAST(NULL AS UNIQUEIDENTIFIER),
                    Message = CAST(N'Work item completed.' AS NVARCHAR(4000))
                FROM [{schema}].[WorkItems] w
                WHERE w.CompletedAt IS NOT NULL

                UNION ALL

                SELECT
                    c.RunId,
                    OccurredAt = c.CreatedAt,
                    EventType = CAST(N'CheckpointRecorded' AS NVARCHAR(200)),
                    Source = CAST(N'MigrationCheckpoints' AS NVARCHAR(200)),
                    WorkItemId = CAST(NULL AS UNIQUEIDENTIFIER),
                    ManifestRecordId = CAST(NULL AS UNIQUEIDENTIFIER),
                    CheckpointId = c.CheckpointId,
                    FailureId = CAST(NULL AS UNIQUEIDENTIFIER),
                    Message = CAST(CASE
                        WHEN c.CheckpointValue IS NULL THEN c.CheckpointName
                        ELSE c.CheckpointName + N': ' + c.CheckpointValue
                    END AS NVARCHAR(4000))
                FROM [{schema}].[MigrationCheckpoints] c

                UNION ALL

                SELECT
                    f.RunId,
                    OccurredAt = f.CreatedAt,
                    EventType = CAST(N'FailureRecorded:' + COALESCE(f.FailureType, N'Failure') AS NVARCHAR(200)),
                    Source = CAST(N'MigrationFailures' AS NVARCHAR(200)),
                    WorkItemId = f.WorkItemId,
                    ManifestRecordId = f.ManifestRecordId,
                    CheckpointId = CAST(NULL AS UNIQUEIDENTIFIER),
                    FailureId = f.FailureId,
                    Message = CAST(COALESCE(f.Message, N'Failure recorded.') AS NVARCHAR(4000))
                FROM [{schema}].[MigrationFailures] f
            ) activity
            WHERE OccurredAt IS NOT NULL
            ORDER BY OccurredAt DESC, EventType;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Limit", safeLimit);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var events = new List<OperationalGlobalActivityEvent>();

        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(new OperationalGlobalActivityEvent
            {
                RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
                OccurredAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("OccurredAt")),
                EventType = reader.GetString(reader.GetOrdinal("EventType")),
                Source = reader.GetString(reader.GetOrdinal("Source")),
                WorkItemId = ReadNullableGuid(reader, "WorkItemId"),
                ManifestRecordId = ReadNullableGuid(reader, "ManifestRecordId"),
                CheckpointId = ReadNullableGuid(reader, "CheckpointId"),
                FailureId = ReadNullableGuid(reader, "FailureId"),
                Message = reader.GetString(reader.GetOrdinal("Message"))
            });
        }

        return new OperationalGlobalActivityFeedResponse
        {
            EventCount = events.Count,
            Limit = safeLimit,
            GeneratedAt = DateTimeOffset.UtcNow,
            Events = events
        };
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_options.Value.SchemaName)
            ? "migration"
            : _options.Value.SchemaName;
    }

    private static Guid? ReadNullableGuid(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }
}


