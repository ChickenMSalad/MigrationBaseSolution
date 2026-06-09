using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalLeaseExpirationService : IOperationalLeaseExpirationService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;
    private readonly IOptions<OperationalLeaseExpirationOptions> _leaseOptions;
    private readonly ILogger<OperationalLeaseExpirationService> _logger;

    public OperationalLeaseExpirationService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions,
        IOptions<OperationalLeaseExpirationOptions> leaseOptions,
        ILogger<OperationalLeaseExpirationService> logger)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
        _leaseOptions = leaseOptions;
        _logger = logger;
    }

    public async Task<OperationalExpiredLeaseListResponse> ListExpiredAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();
        var leaseTimeoutMinutes = GetLeaseTimeoutMinutes();
        var expiresBefore = DateTimeOffset.UtcNow.AddMinutes(-leaseTimeoutMinutes);

        var sql = $"""
            SELECT
                wi.WorkItemId,
                wi.RunId,
                wi.ManifestRecordId,
                wi.Status,
                wi.AttemptCount,
                wi.LockedBy,
                wi.LockedAt,
                m.SourceId,
                m.SourcePath,
                m.SourceName
            FROM [{schema}].[WorkItems] wi
            INNER JOIN [{schema}].[MigrationManifestRecords] m
                ON m.ManifestRecordId = wi.ManifestRecordId
            WHERE wi.Status = N'Locked'
              AND wi.LockedAt IS NOT NULL
              AND wi.LockedAt < @ExpiresBefore
              AND wi.CompletedAt IS NULL
              AND wi.FailedAt IS NULL
            ORDER BY wi.LockedAt, wi.WorkItemId;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ExpiresBefore", expiresBefore);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<OperationalExpiredLeaseItem>();

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadExpiredLeaseItem(reader, expiresBefore));
        }

        return new OperationalExpiredLeaseListResponse
        {
            LeaseTimeoutMinutes = leaseTimeoutMinutes,
            ExpiresBefore = expiresBefore,
            Count = items.Count,
            WorkItems = items
        };
    }

    public async Task<OperationalReclaimExpiredLeasesResponse> ReclaimExpiredAsync(
        OperationalReclaimExpiredLeasesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is required.", nameof(request));
        }

        var schema = GetSchemaName();
        var leaseTimeoutMinutes = GetLeaseTimeoutMinutes();
        var expiresBefore = DateTimeOffset.UtcNow.AddMinutes(-leaseTimeoutMinutes);
        var maxCount = Math.Clamp(
            request.MaxCount ?? _leaseOptions.Value.MaxReclaimCount,
            1,
            Math.Max(1, _leaseOptions.Value.MaxReclaimCount));

        var sql = $"""
            DECLARE @Reclaimed TABLE
            (
                WorkItemId UNIQUEIDENTIFIER NOT NULL
            );

            ;WITH ExpiredWorkItems AS
            (
                SELECT TOP (@MaxCount)
                    WorkItemId
                FROM [{schema}].[WorkItems] WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status = N'Locked'
                  AND LockedAt IS NOT NULL
                  AND LockedAt < @ExpiresBefore
                  AND CompletedAt IS NULL
                  AND FailedAt IS NULL
                ORDER BY LockedAt, WorkItemId
            )
            UPDATE wi
                SET
                    Status = N'Created',
                    LockedAt = NULL,
                    LockedBy = NULL
                OUTPUT inserted.WorkItemId INTO @Reclaimed
            FROM [{schema}].[WorkItems] wi
            INNER JOIN ExpiredWorkItems e
                ON e.WorkItemId = wi.WorkItemId;

            SELECT WorkItemId
            FROM @Reclaimed
            ORDER BY WorkItemId;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@MaxCount", maxCount);
        command.Parameters.AddWithValue("@ExpiresBefore", expiresBefore);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var workItemIds = new List<Guid>();

        while (await reader.ReadAsync(cancellationToken))
        {
            workItemIds.Add(reader.GetGuid(reader.GetOrdinal("WorkItemId")));
        }

        _logger.LogWarning(
            "Reclaimed {Count} expired operational work item leases. Reason: {Reason}",
            workItemIds.Count,
            request.Reason.Trim());

        return new OperationalReclaimExpiredLeasesResponse
        {
            LeaseTimeoutMinutes = leaseTimeoutMinutes,
            ExpiresBefore = expiresBefore,
            RequestedMaxCount = maxCount,
            ReclaimedCount = workItemIds.Count,
            WorkItemIds = workItemIds,
            Reason = request.Reason.Trim()
        };
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName)
            ? "migration"
            : _sqlOptions.Value.SchemaName;
    }

    private int GetLeaseTimeoutMinutes()
    {
        return Math.Max(1, _leaseOptions.Value.LeaseTimeoutMinutes);
    }

    private static OperationalExpiredLeaseItem ReadExpiredLeaseItem(
        SqlDataReader reader,
        DateTimeOffset expiresBefore)
    {
        return new OperationalExpiredLeaseItem
        {
            WorkItemId = reader.GetInt64(reader.GetOrdinal("WorkItemId")),
            RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
            ManifestRecordId = reader.GetInt64(reader.GetOrdinal("ManifestRecordId")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            AttemptCount = reader.GetInt32(reader.GetOrdinal("AttemptCount")),
            LockedBy = ReadNullableString(reader, "LockedBy"),
            LockedAt = ReadNullableDateTimeOffset(reader, "LockedAt"),
            ExpiresBefore = expiresBefore,
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


