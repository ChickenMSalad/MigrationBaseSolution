using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineService : IOperationalRunTimelineService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public OperationalRunTimelineService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options;
    }

    public async Task<OperationalRunTimelineResponse?> GetTimelineAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            throw new ArgumentException("RunId is required.", nameof(runId));
        }

        var schema = GetSchemaName();

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var runExists = await RunExistsAsync(connection, schema, runId, cancellationToken);

        if (!runExists)
        {
            return null;
        }

        var events = new List<OperationalRunTimelineEvent>();

        events.AddRange(await ReadRunEventsAsync(connection, schema, runId, cancellationToken));
        events.AddRange(await ReadWorkItemEventsAsync(connection, schema, runId, cancellationToken));
        events.AddRange(await ReadCheckpointEventsAsync(connection, schema, runId, cancellationToken));
        events.AddRange(await ReadFailureEventsAsync(connection, schema, runId, cancellationToken));

        var ordered = events
            .OrderBy(e => e.OccurredAt)
            .ThenBy(e => e.EventType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new OperationalRunTimelineResponse
        {
            RunId = runId,
            EventCount = ordered.Length,
            Events = ordered
        };
    }

    private static async Task<bool> RunExistsAsync(
        SqlConnection connection,
        string schema,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT COUNT(1)
            FROM [{schema}].[MigrationRuns]
            WHERE RunId = @RunId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<IReadOnlyCollection<OperationalRunTimelineEvent>> ReadRunEventsAsync(
        SqlConnection connection,
        string schema,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                RunId,
                Status,
                CreatedAt,
                StartedAt,
                CompletedAt,
                FailedAt,
                FailureReason
            FROM [{schema}].[MigrationRuns]
            WHERE RunId = @RunId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var events = new List<OperationalRunTimelineEvent>();

        if (!await reader.ReadAsync(cancellationToken))
        {
            return events;
        }

        var status = reader.GetString(reader.GetOrdinal("Status"));
        var failureReason = ReadNullableString(reader, "FailureReason");

        events.Add(new OperationalRunTimelineEvent
        {
            OccurredAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
            EventType = "RunCreated",
            Source = "MigrationRuns",
            RunId = runId,
            Message = $"Operational run created with status {status}."
        });

        var startedAt = ReadNullableDateTimeOffset(reader, "StartedAt");
        if (startedAt is not null)
        {
            events.Add(new OperationalRunTimelineEvent
            {
                OccurredAt = startedAt.Value,
                EventType = "RunStarted",
                Source = "MigrationRuns",
                RunId = runId,
                Message = "Operational run started."
            });
        }

        var completedAt = ReadNullableDateTimeOffset(reader, "CompletedAt");
        if (completedAt is not null)
        {
            events.Add(new OperationalRunTimelineEvent
            {
                OccurredAt = completedAt.Value,
                EventType = "RunCompleted",
                Source = "MigrationRuns",
                RunId = runId,
                Message = "Operational run completed."
            });
        }

        var failedAt = ReadNullableDateTimeOffset(reader, "FailedAt");
        if (failedAt is not null)
        {
            events.Add(new OperationalRunTimelineEvent
            {
                OccurredAt = failedAt.Value,
                EventType = "RunFailed",
                Source = "MigrationRuns",
                RunId = runId,
                Message = string.IsNullOrWhiteSpace(failureReason)
                    ? "Operational run failed."
                    : failureReason
            });
        }

        return events;
    }

    private static async Task<IReadOnlyCollection<OperationalRunTimelineEvent>> ReadWorkItemEventsAsync(
        SqlConnection connection,
        string schema,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                WorkItemId,
                ManifestRecordId,
                Status,
                CreatedAt,
                LockedAt,
                CompletedAt,
                FailedAt,
                LockedBy,
                LastFailureReason
            FROM [{schema}].[MigrationWorkItems]
            WHERE RunId = @RunId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var events = new List<OperationalRunTimelineEvent>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var workItemId = reader.GetGuid(reader.GetOrdinal("WorkItemId"));
            var manifestRecordId = reader.GetGuid(reader.GetOrdinal("ManifestRecordId"));
            var status = reader.GetString(reader.GetOrdinal("Status"));
            var lockedBy = ReadNullableString(reader, "LockedBy");
            var failureReason = ReadNullableString(reader, "LastFailureReason");

            events.Add(new OperationalRunTimelineEvent
            {
                OccurredAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
                EventType = "WorkItemCreated",
                Source = "MigrationWorkItems",
                RunId = runId,
                WorkItemId = workItemId,
                ManifestRecordId = manifestRecordId,
                Message = $"Work item created with status {status}."
            });

            var lockedAt = ReadNullableDateTimeOffset(reader, "LockedAt");
            if (lockedAt is not null)
            {
                events.Add(new OperationalRunTimelineEvent
                {
                    OccurredAt = lockedAt.Value,
                    EventType = "WorkItemLocked",
                    Source = "MigrationWorkItems",
                    RunId = runId,
                    WorkItemId = workItemId,
                    ManifestRecordId = manifestRecordId,
                    Message = string.IsNullOrWhiteSpace(lockedBy)
                        ? "Work item locked."
                        : $"Work item locked by {lockedBy}."
                });
            }

            var completedAt = ReadNullableDateTimeOffset(reader, "CompletedAt");
            if (completedAt is not null)
            {
                events.Add(new OperationalRunTimelineEvent
                {
                    OccurredAt = completedAt.Value,
                    EventType = "WorkItemCompleted",
                    Source = "MigrationWorkItems",
                    RunId = runId,
                    WorkItemId = workItemId,
                    ManifestRecordId = manifestRecordId,
                    Message = "Work item completed."
                });
            }

            var failedAt = ReadNullableDateTimeOffset(reader, "FailedAt");
            if (failedAt is not null)
            {
                events.Add(new OperationalRunTimelineEvent
                {
                    OccurredAt = failedAt.Value,
                    EventType = "WorkItemFailed",
                    Source = "MigrationWorkItems",
                    RunId = runId,
                    WorkItemId = workItemId,
                    ManifestRecordId = manifestRecordId,
                    Message = string.IsNullOrWhiteSpace(failureReason)
                        ? "Work item failed."
                        : failureReason
                });
            }
        }

        return events;
    }

    private static async Task<IReadOnlyCollection<OperationalRunTimelineEvent>> ReadCheckpointEventsAsync(
        SqlConnection connection,
        string schema,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                CheckpointId,
                CheckpointName,
                CheckpointValue,
                CreatedAt,
                UpdatedAt
            FROM [{schema}].[MigrationCheckpoints]
            WHERE RunId = @RunId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var events = new List<OperationalRunTimelineEvent>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var checkpointName = ReadNullableString(reader, "CheckpointName") ?? "Checkpoint";
            var checkpointValue = ReadNullableString(reader, "CheckpointValue");

            events.Add(new OperationalRunTimelineEvent
            {
                OccurredAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
                EventType = "CheckpointRecorded",
                Source = "MigrationCheckpoints",
                RunId = runId,
                CheckpointId = reader.GetGuid(reader.GetOrdinal("CheckpointId")),
                Message = string.IsNullOrWhiteSpace(checkpointValue)
                    ? checkpointName
                    : $"{checkpointName}: {checkpointValue}"
            });
        }

        return events;
    }

    private static async Task<IReadOnlyCollection<OperationalRunTimelineEvent>> ReadFailureEventsAsync(
        SqlConnection connection,
        string schema,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                FailureId,
                ManifestRecordId,
                WorkItemId,
                FailureType,
                Message,
                Details,
                IsRetriable,
                CreatedAt
            FROM [{schema}].[MigrationFailures]
            WHERE RunId = @RunId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var events = new List<OperationalRunTimelineEvent>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var failureType = ReadNullableString(reader, "FailureType") ?? "Failure";
            var message = ReadNullableString(reader, "Message") ?? "Failure recorded.";
            var isRetriable = reader.IsDBNull(reader.GetOrdinal("IsRetriable"))
                ? (bool?)null
                : reader.GetBoolean(reader.GetOrdinal("IsRetriable"));

            var suffix = isRetriable is null
                ? string.Empty
                : isRetriable.Value ? " Retriable." : " Not retriable.";

            events.Add(new OperationalRunTimelineEvent
            {
                OccurredAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
                EventType = $"FailureRecorded:{failureType}",
                Source = "MigrationFailures",
                RunId = runId,
                WorkItemId = ReadNullableGuid(reader, "WorkItemId"),
                ManifestRecordId = ReadNullableGuid(reader, "ManifestRecordId"),
                FailureId = reader.GetGuid(reader.GetOrdinal("FailureId")),
                Message = message + suffix
            });
        }

        return events;
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_options.Value.SchemaName)
            ? "migration"
            : _options.Value.SchemaName;
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }

    private static Guid? ReadNullableGuid(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }
}
