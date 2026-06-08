using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorReadService : IOperationalMirrorReadService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public OperationalMirrorReadService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options;
    }

    public async Task<IReadOnlyCollection<OperationalMirrorRunSummary>> ListRunsAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        var sql = $"""
            SELECT
                r.RunId,
                r.SourceSystem,
                r.TargetSystem,
                r.Status,
                r.CreatedAt,
                ManifestRecordCount = (
                    SELECT COUNT(1)
                    FROM [{schema}].[MigrationManifestRecords] m
                    WHERE m.RunId = r.RunId
                ),
                WorkItemCount = (
                    SELECT COUNT(1)
                    FROM [{schema}].[MigrationWorkItems] w
                    WHERE w.RunId = r.RunId
                ),
                CheckpointCount = (
                    SELECT COUNT(1)
                    FROM [{schema}].[MigrationCheckpoints] c
                    WHERE c.RunId = r.RunId
                )
            FROM [{schema}].[MigrationRuns] r
            ORDER BY r.CreatedAt DESC;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<OperationalMirrorRunSummary>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OperationalMirrorRunSummary
            {
                RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
                SourceSystem = reader.GetString(reader.GetOrdinal("SourceSystem")),
                TargetSystem = reader.GetString(reader.GetOrdinal("TargetSystem")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
                ManifestRecordCount = reader.GetInt32(reader.GetOrdinal("ManifestRecordCount")),
                WorkItemCount = reader.GetInt32(reader.GetOrdinal("WorkItemCount")),
                CheckpointCount = reader.GetInt32(reader.GetOrdinal("CheckpointCount"))
            });
        }

        return results;
    }

    public async Task<OperationalMirrorRunDetailResponse?> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var runs = await ListRunsAsync(cancellationToken);
        var run = runs.FirstOrDefault(x => x.RunId == runId);

        if (run is null)
        {
            return null;
        }

        var schema = GetSchemaName();

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var manifestRecords = await ReadManifestRecordsAsync(
            connection,
            schema,
            runId,
            cancellationToken);

        var workItems = await ReadWorkItemsAsync(
            connection,
            schema,
            runId,
            cancellationToken);

        var checkpoints = await ReadCheckpointsAsync(
            connection,
            schema,
            runId,
            cancellationToken);

        return new OperationalMirrorRunDetailResponse
        {
            Run = run,
            ManifestRecords = manifestRecords,
            WorkItems = workItems,
            Checkpoints = checkpoints
        };
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_options.Value.SchemaName)
            ? "migration"
            : _options.Value.SchemaName;
    }

    private static async Task<IReadOnlyCollection<OperationalMirrorManifestRecordItem>> ReadManifestRecordsAsync(
        SqlConnection connection,
        string schema,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                ManifestRecordId,
                RunId,
                SequenceNumber,
                SourceId,
                SourcePath,
                SourceName,
                Status,
                CreatedAt,
                UpdatedAt
            FROM [{schema}].[MigrationManifestRecords]
            WHERE RunId = @RunId
            ORDER BY SequenceNumber;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<OperationalMirrorManifestRecordItem>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OperationalMirrorManifestRecordItem
            {
                ManifestRecordId = reader.GetInt64(reader.GetOrdinal("ManifestRecordId")),
                RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
                SequenceNumber = reader.GetInt64(reader.GetOrdinal("SequenceNumber")),
                SourceId = reader.GetString(reader.GetOrdinal("SourceId")),
                SourcePath = ReadNullableString(reader, "SourcePath"),
                SourceName = ReadNullableString(reader, "SourceName"),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = ReadNullableDateTimeOffset(reader, "UpdatedAt")
            });
        }

        return results;
    }

    private static async Task<IReadOnlyCollection<OperationalMirrorWorkItemItem>> ReadWorkItemsAsync(
        SqlConnection connection,
        string schema,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                WorkItemId,
                RunId,
                ManifestRecordId,
                Status,
                AttemptCount,
                LockedBy,
                CreatedAt
            FROM [{schema}].[MigrationWorkItems]
            WHERE RunId = @RunId
            ORDER BY CreatedAt;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<OperationalMirrorWorkItemItem>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OperationalMirrorWorkItemItem
            {
                WorkItemId = reader.GetInt64(reader.GetOrdinal("WorkItemId")),
                RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
                ManifestRecordId = reader.GetInt64(reader.GetOrdinal("ManifestRecordId")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                AttemptCount = reader.GetInt32(reader.GetOrdinal("AttemptCount")),
                LockedBy = ReadNullableString(reader, "LockedBy"),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt"))
            });
        }

        return results;
    }

    private static async Task<IReadOnlyCollection<OperationalMirrorCheckpointItem>> ReadCheckpointsAsync(
        SqlConnection connection,
        string schema,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                CheckpointId,
                RunId,
                CheckpointName,
                CheckpointValue,
                CreatedAt,
                UpdatedAt
            FROM [{schema}].[MigrationCheckpoints]
            WHERE RunId = @RunId
            ORDER BY CheckpointName;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<OperationalMirrorCheckpointItem>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OperationalMirrorCheckpointItem
            {
                CheckpointId = reader.GetGuid(reader.GetOrdinal("CheckpointId")),
                RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
                CheckpointName = reader.GetString(reader.GetOrdinal("CheckpointName")),
                CheckpointValue = reader.GetString(reader.GetOrdinal("CheckpointValue")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = ReadNullableDateTimeOffset(reader, "UpdatedAt")
            });
        }

        return results;
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
