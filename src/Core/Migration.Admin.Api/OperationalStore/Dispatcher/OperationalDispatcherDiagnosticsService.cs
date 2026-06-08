using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherDiagnosticsService
    : IOperationalDispatcherDiagnosticsService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;
    private readonly IOptions<OperationalLeaseExpirationOptions> _leaseOptions;
    private readonly IOptions<OperationalDispatcherOptions> _dispatcherOptions;

    public OperationalDispatcherDiagnosticsService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions,
        IOptions<OperationalLeaseExpirationOptions> leaseOptions,
        IOptions<OperationalDispatcherOptions> dispatcherOptions)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
        _leaseOptions = leaseOptions;
        _dispatcherOptions = dispatcherOptions;
    }

    public async Task<OperationalDispatcherDiagnosticsResponse> GetDiagnosticsAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();
        var leaseTimeoutMinutes = Math.Max(1, _leaseOptions.Value.LeaseTimeoutMinutes);
        var expiresBefore = DateTimeOffset.UtcNow.AddMinutes(-leaseTimeoutMinutes);
        var previewCount = Math.Clamp(_dispatcherOptions.Value.LeaseCount, 1, 25);

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var summary = await ReadSummaryAsync(
            connection,
            schema,
            expiresBefore,
            cancellationToken);

        var preview = await ReadEligiblePreviewAsync(
            connection,
            schema,
            previewCount,
            cancellationToken);

        return new OperationalDispatcherDiagnosticsResponse
        {
            EligibleWorkItemCount = summary.EligibleWorkItemCount,
            BlockedByRunStateCount = summary.BlockedByRunStateCount,
            LockedWorkItemCount = summary.LockedWorkItemCount,
            CompletedWorkItemCount = summary.CompletedWorkItemCount,
            FailedWorkItemCount = summary.FailedWorkItemCount,
            ExpiredLeaseCount = summary.ExpiredLeaseCount,
            EligiblePreview = preview,
            Message = summary.EligibleWorkItemCount == 0
                ? "No eligible dispatcher work items are currently available."
                : $"{summary.EligibleWorkItemCount} dispatcher work item(s) are eligible."
        };
    }

    private static async Task<OperationalDispatcherDiagnosticsSummary> ReadSummaryAsync(
        SqlConnection connection,
        string schema,
        DateTimeOffset expiresBefore,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                EligibleWorkItemCount = SUM(CASE
                    WHEN wi.Status = N'Created'
                     AND wi.LockedBy IS NULL
                     AND wi.LockedAt IS NULL
                     AND r.Status NOT IN (N'CancelRequested', N'Aborted', N'Archived', N'Completed', N'Failed', N'Canceled')
                    THEN 1 ELSE 0 END),
                BlockedByRunStateCount = SUM(CASE
                    WHEN wi.Status = N'Created'
                     AND wi.LockedBy IS NULL
                     AND wi.LockedAt IS NULL
                     AND r.Status IN (N'CancelRequested', N'Aborted', N'Archived', N'Completed', N'Failed', N'Canceled')
                    THEN 1 ELSE 0 END),
                LockedWorkItemCount = SUM(CASE WHEN wi.Status = N'Locked' THEN 1 ELSE 0 END),
                CompletedWorkItemCount = SUM(CASE WHEN wi.Status = N'Completed' THEN 1 ELSE 0 END),
                FailedWorkItemCount = SUM(CASE WHEN wi.Status = N'Failed' THEN 1 ELSE 0 END),
                ExpiredLeaseCount = SUM(CASE
                    WHEN wi.Status = N'Locked'
                     AND wi.LockedAt IS NOT NULL
                     AND wi.LockedAt < @ExpiresBefore
                     AND wi.CompletedAt IS NULL
                     AND wi.FailedAt IS NULL
                    THEN 1 ELSE 0 END)
            FROM [{schema}].[MigrationWorkItems] wi
            INNER JOIN [{schema}].[MigrationRuns] r
                ON r.RunId = wi.RunId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ExpiresBefore", expiresBefore);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new OperationalDispatcherDiagnosticsSummary();
        }

        return new OperationalDispatcherDiagnosticsSummary
        {
            EligibleWorkItemCount = ReadInt32(reader, "EligibleWorkItemCount"),
            BlockedByRunStateCount = ReadInt32(reader, "BlockedByRunStateCount"),
            LockedWorkItemCount = ReadInt32(reader, "LockedWorkItemCount"),
            CompletedWorkItemCount = ReadInt32(reader, "CompletedWorkItemCount"),
            FailedWorkItemCount = ReadInt32(reader, "FailedWorkItemCount"),
            ExpiredLeaseCount = ReadInt32(reader, "ExpiredLeaseCount")
        };
    }

    private static async Task<IReadOnlyCollection<OperationalDispatcherEligibleWorkItemPreview>> ReadEligiblePreviewAsync(
        SqlConnection connection,
        string schema,
        int previewCount,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT TOP (@PreviewCount)
                wi.WorkItemId,
                wi.RunId,
                wi.ManifestRecordId,
                RunStatus = r.Status,
                wi.AttemptCount,
                wi.CreatedAt,
                m.SourceId,
                m.SourceName
            FROM [{schema}].[MigrationWorkItems] wi
            INNER JOIN [{schema}].[MigrationRuns] r
                ON r.RunId = wi.RunId
            INNER JOIN [{schema}].[MigrationManifestRecords] m
                ON m.ManifestRecordId = wi.ManifestRecordId
            WHERE wi.Status = N'Created'
              AND wi.LockedBy IS NULL
              AND wi.LockedAt IS NULL
              AND r.Status NOT IN (N'CancelRequested', N'Aborted', N'Archived', N'Completed', N'Failed', N'Canceled')
            ORDER BY wi.CreatedAt, wi.WorkItemId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PreviewCount", previewCount);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<OperationalDispatcherEligibleWorkItemPreview>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OperationalDispatcherEligibleWorkItemPreview
            {
                WorkItemId = GetLong(reader, "WorkItemId"),
                RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
                ManifestRecordId = GetLong(reader, "ManifestRecordId"),
                RunStatus = reader.GetString(reader.GetOrdinal("RunStatus")),
                AttemptCount = ReadInt32(reader, "AttemptCount"),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
                SourceId = reader.GetString(reader.GetOrdinal("SourceId")),
                SourceName = ReadNullableString(reader, "SourceName")
            });
        }

        return results;
    }

    private static long GetLong(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt64(reader.GetValue(ordinal));
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName)
            ? "migration"
            : _sqlOptions.Value.SchemaName;
    }

    private static int ReadInt32(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private sealed class OperationalDispatcherDiagnosticsSummary
    {
        public int EligibleWorkItemCount { get; init; }

        public int BlockedByRunStateCount { get; init; }

        public int LockedWorkItemCount { get; init; }

        public int CompletedWorkItemCount { get; init; }

        public int FailedWorkItemCount { get; init; }

        public int ExpiredLeaseCount { get; init; }
    }
}
