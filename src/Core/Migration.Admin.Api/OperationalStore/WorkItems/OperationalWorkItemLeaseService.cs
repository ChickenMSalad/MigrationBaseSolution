using Migration.Infrastructure.State.OperationalStore.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalWorkItemLeaseService : IOperationalWorkItemLeaseService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public OperationalWorkItemLeaseService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options;
    }

    public async Task<OperationalWorkItemLeaseResponse> LeaseAsync(
        OperationalWorkItemLeaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.WorkerId))
        {
            throw new ArgumentException("WorkerId is required.", nameof(request));
        }

        var requestedCount = Math.Clamp(request.Count, 1, 100);
        var schema = GetSchemaName();
        var workerId = request.WorkerId.Trim();

        var sql = $"""
            DECLARE @Leased TABLE
            (
                WorkItemId UNIQUEIDENTIFIER NOT NULL,
                RunId UNIQUEIDENTIFIER NOT NULL,
                ManifestRecordId UNIQUEIDENTIFIER NOT NULL,
                Status NVARCHAR(64) NOT NULL,
                AttemptCount INT NOT NULL,
                LockedBy NVARCHAR(256) NULL,
                LockedAt DATETIMEOFFSET NULL
            );

            ;WITH CandidateWorkItems AS
            (
                SELECT TOP (@Count)
                    wi.WorkItemId
                FROM [{schema}].[MigrationWorkItems] wi WITH (UPDLOCK, READPAST, ROWLOCK)
                INNER JOIN [{schema}].[MigrationRuns] r
                    ON r.RunId = wi.RunId
                WHERE wi.Status = N'Created'
                  AND wi.LockedBy IS NULL
                  AND wi.LockedAt IS NULL
                  AND r.Status NOT IN (N'CancelRequested', N'Aborted')
                ORDER BY wi.CreatedAt, wi.WorkItemId
            )
            UPDATE wi
                SET
                    Status = N'Locked',
                    LockedBy = @WorkerId,
                    LockedAt = SYSDATETIMEOFFSET(),
                    AttemptCount = AttemptCount + 1
                OUTPUT
                    inserted.WorkItemId,
                    inserted.RunId,
                    inserted.ManifestRecordId,
                    inserted.Status,
                    inserted.AttemptCount,
                    inserted.LockedBy,
                    inserted.LockedAt
                INTO @Leased
            FROM [{schema}].[MigrationWorkItems] wi
            INNER JOIN CandidateWorkItems c
                ON c.WorkItemId = wi.WorkItemId;

            SELECT
                l.WorkItemId,
                l.RunId,
                l.ManifestRecordId,
                l.Status,
                l.AttemptCount,
                l.LockedBy,
                l.LockedAt,
                m.SourceId,
                m.SourcePath,
                m.SourceName
            FROM @Leased l
            INNER JOIN [{schema}].[MigrationManifestRecords] m
                ON m.ManifestRecordId = l.ManifestRecordId
            ORDER BY l.LockedAt, l.WorkItemId;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Count", requestedCount);
        command.Parameters.AddWithValue("@WorkerId", workerId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<OperationalWorkItemLeaseItem>();

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadLeaseItem(reader));
        }

        var message = items.Count == 0
            ? "No eligible operational work items were available to lease. Runs in CancelRequested or Aborted state are blocked from new leases."
            : "Operational work items leased.";

        return new OperationalWorkItemLeaseResponse
        {
            WorkerId = workerId,
            RequestedCount = requestedCount,
            LeasedCount = items.Count,
            LeaseBlocked = items.Count == 0,
            Message = message,
            WorkItems = items
        };
    }

    public Task<OperationalWorkItemStateTransitionResponse> HeartbeatAsync(
        long workItemId,
        OperationalWorkItemHeartbeatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return UpdateOwnedWorkItemAsync(
            workItemId,
            request.WorkerId,
            "Locked",
            "Locked",
            setClause: "LockedAt = SYSDATETIMEOFFSET()",
            successMessage: "Work item heartbeat recorded.",
            cancellationToken);
    }

    public Task<OperationalWorkItemStateTransitionResponse> CompleteAsync(
        long workItemId,
        OperationalWorkItemCompleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return UpdateOwnedWorkItemAsync(
            workItemId,
            request.WorkerId,
            "Locked",
            "Completed",
            setClause: "Status = N'Completed', CompletedAt = SYSDATETIMEOFFSET()",
            successMessage: "Work item completed.",
            cancellationToken);
    }

    public async Task<OperationalWorkItemStateTransitionResponse> FailAsync(
        long workItemId,
        OperationalWorkItemFailRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FailureReason))
        {
            throw new ArgumentException("FailureReason is required.", nameof(request));
        }

        return await UpdateOwnedWorkItemAsync(
            workItemId,
            request.WorkerId,
            "Locked",
            "Failed",
            setClause: "Status = N'Failed', FailedAt = SYSDATETIMEOFFSET(), LastFailureReason = @FailureReason",
            successMessage: "Work item failed.",
            cancellationToken,
            failureReason: request.FailureReason.Trim());
    }

    private async Task<OperationalWorkItemStateTransitionResponse> UpdateOwnedWorkItemAsync(
        long workItemId,
        string workerId,
        string requiredStatus,
        string successStatus,
        string setClause,
        string successMessage,
        CancellationToken cancellationToken,
        string? failureReason = null)
    {
        if (workItemId <= 0)
        {
            throw new ArgumentException("WorkItemId is required.", nameof(workItemId));
        }

        if (string.IsNullOrWhiteSpace(workerId))
        {
            throw new ArgumentException("WorkerId is required.", nameof(workerId));
        }

        var schema = GetSchemaName();

        var sql = $"""
            UPDATE [{schema}].[MigrationWorkItems]
                SET {setClause}
            OUTPUT
                inserted.WorkItemId,
                inserted.Status,
                inserted.LockedBy,
                inserted.LockedAt
            WHERE WorkItemId = @WorkItemId
              AND LockedBy = @WorkerId
              AND Status = @RequiredStatus;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@WorkItemId", workItemId);
        command.Parameters.AddWithValue("@WorkerId", workerId.Trim());
        command.Parameters.AddWithValue("@RequiredStatus", requiredStatus);

        if (failureReason is not null)
        {
            command.Parameters.AddWithValue("@FailureReason", failureReason);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new OperationalWorkItemStateTransitionResponse
            {
                WorkItemId = workItemId,
                Success = false,
                Message = "Work item was not updated. It may not exist, may not be leased by this worker, or may not be in the required state."
            };
        }

        return new OperationalWorkItemStateTransitionResponse
        {
            WorkItemId = reader.GetInt64(reader.GetOrdinal("WorkItemId")),
            Success = true,
            Message = successMessage,
            Status = successStatus,
            LockedBy = ReadNullableString(reader, "LockedBy"),
            LockedAt = ReadNullableDateTimeOffset(reader, "LockedAt")
        };
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_options.Value.SchemaName)
            ? "migration"
            : _options.Value.SchemaName;
    }

    private static OperationalWorkItemLeaseItem ReadLeaseItem(
        SqlDataReader reader)
    {
        return new OperationalWorkItemLeaseItem
        {
            WorkItemId = reader.GetInt64(reader.GetOrdinal("WorkItemId")),
            RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
            ManifestRecordId = reader.GetInt64(reader.GetOrdinal("ManifestRecordId")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            AttemptCount = reader.GetInt32(reader.GetOrdinal("AttemptCount")),
            LockedBy = ReadNullableString(reader, "LockedBy"),
            LockedAt = ReadNullableDateTimeOffset(reader, "LockedAt"),
            SourceId = reader.GetString(reader.GetOrdinal("SourceId")),
            SourcePath = ReadNullableString(reader, "SourcePath"),
            SourceName = ReadNullableString(reader, "SourceName")
        };
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
