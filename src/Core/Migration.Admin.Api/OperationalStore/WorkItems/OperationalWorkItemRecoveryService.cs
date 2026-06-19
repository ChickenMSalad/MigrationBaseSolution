using Migration.Infrastructure.Sql.Connections;
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalWorkItemRecoveryService : IOperationalWorkItemRecoveryService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;
    private readonly ILogger<OperationalWorkItemRecoveryService> _logger;

    public OperationalWorkItemRecoveryService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options,
        ILogger<OperationalWorkItemRecoveryService> logger)
    {
        _connectionFactory = connectionFactory;
        _options = options;
        _logger = logger;
    }

    public Task<OperationalWorkItemStateTransitionResponse> ReleaseAsync(
        long workItemId,
        OperationalWorkItemReleaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.WorkerId))
        {
            throw new ArgumentException("WorkerId is required.", nameof(request));
        }

        return UpdateAsync(
            workItemId,
            setClause: """
                Status = N'Queued',
                LeaseOwner = NULL,
                LeaseExpiresUtc = NULL,
                UpdatedUtc = SYSUTCDATETIME()
                """,
            whereClause: """
                WorkItemId = @WorkItemId
                AND Status IN (N'Locked', N'Leased', N'Dispatching', N'Dispatched', N'Running')
                AND (LeaseOwner = @WorkerId OR @WorkerId = N'admin-ui')
                """,
            configureCommand: command =>
            {
                command.Parameters.Add(new SqlParameter("@WorkerId", SqlDbType.NVarChar, 256) { Value = request.WorkerId.Trim() });
            },
            afterUpdateAsync: null,
            successStatus: "Queued",
            successMessage: "Work item lease released back to Queued.",
            notUpdatedMessage: "Work item was not released. It may not exist, may not be active, or may not be owned by the supplied worker.",
            cancellationToken);
    }

    public Task<OperationalWorkItemStateTransitionResponse> ResetAsync(
        long workItemId,
        OperationalWorkItemResetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is required.", nameof(request));
        }

        _logger.LogWarning(
            "Operational work item {WorkItemId} retry reset requested. Reason: {Reason}",
            workItemId,
            request.Reason.Trim());

        return UpdateAsync(
            workItemId,
            setClause: BuildResetSetClause(),
            whereClause: "WorkItemId = @WorkItemId",
            configureCommand: _ => { },
            afterUpdateAsync: MarkOwningRunQueuedAsync,
            successStatus: "Queued",
            successMessage: "Work item reset to Queued for retry.",
            notUpdatedMessage: "Work item was not reset. It may not exist.",
            cancellationToken);
    }

    private string BuildResetSetClause()
    {
        // Keep this SQL limited to known columns and guard historical columns with COL_LENGTH.
        // The production migration.WorkItems shape has LastErrorCode/LastErrorMessage and may not have LastFailureReason.
        return """
            Status = N'Queued',
            LeaseOwner = NULL,
            LeaseExpiresUtc = NULL,
            NotBeforeUtc = NULL,
            AttemptCount = 0,
            LastErrorCode = NULL,
            LastErrorMessage = NULL,
            StartedAtUtc = NULL,
            CompletedAtUtc = NULL,
            UpdatedUtc = SYSUTCDATETIME()
            """;
    }

    private async Task MarkOwningRunQueuedAsync(
        SqlConnection connection,
        long workItemId,
        CancellationToken cancellationToken)
    {
        var schema = GetSchemaName();
        var runsObjectName = schema + ".Runs";

        var operationalSql = $@"
DECLARE @UpdateSql nvarchar(max) = N'UPDATE [{schema}].[Runs] SET Status = N''Queued''';

IF COL_LENGTH(@RunsObjectName, N'StatusReason') IS NOT NULL
    SET @UpdateSql = @UpdateSql + N', StatusReason = N''Retry requested for failed work item.''';

IF COL_LENGTH(@RunsObjectName, N'CompletedAtUtc') IS NOT NULL
    SET @UpdateSql = @UpdateSql + N', CompletedAtUtc = NULL';

IF COL_LENGTH(@RunsObjectName, N'FailedAt') IS NOT NULL
    SET @UpdateSql = @UpdateSql + N', FailedAt = NULL';

IF COL_LENGTH(@RunsObjectName, N'FailedAtUtc') IS NOT NULL
    SET @UpdateSql = @UpdateSql + N', FailedAtUtc = NULL';

IF COL_LENGTH(@RunsObjectName, N'UpdatedAtUtc') IS NOT NULL
    SET @UpdateSql = @UpdateSql + N', UpdatedAtUtc = SYSUTCDATETIME()';
ELSE IF COL_LENGTH(@RunsObjectName, N'UpdatedUtc') IS NOT NULL
    SET @UpdateSql = @UpdateSql + N', UpdatedUtc = SYSUTCDATETIME()';

SET @UpdateSql = @UpdateSql + N'
WHERE RunId IN (
    SELECT RunId FROM [{schema}].[WorkItems] WHERE WorkItemId = @InnerWorkItemId
)
  AND Status NOT IN (N''Canceled'', N''Cancelling'');';

EXEC sp_executesql
    @UpdateSql,
    N'@InnerWorkItemId bigint',
    @InnerWorkItemId = @WorkItemId;";

        await using (var command = new SqlCommand(operationalSql, connection))
        {
            command.Parameters.Add(new SqlParameter("@WorkItemId", SqlDbType.BigInt) { Value = workItemId });
            command.Parameters.Add(new SqlParameter("@RunsObjectName", SqlDbType.NVarChar, 256) { Value = runsObjectName });
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var adminSql = $@"
IF OBJECT_ID(N'dbo.AdminRuns', N'U') IS NOT NULL
BEGIN
    DECLARE @RunKey nvarchar(256);
    DECLARE @RunGuid nvarchar(36);

    SELECT
        @RunKey = r.RunKey,
        @RunGuid = CONVERT(nvarchar(36), r.RunId)
    FROM [{schema}].[Runs] r
    INNER JOIN [{schema}].[WorkItems] wi ON wi.RunId = r.RunId
    WHERE wi.WorkItemId = @WorkItemId;

    UPDATE dbo.AdminRuns
    SET Status = N'Queued',
        UpdatedUtc = SYSUTCDATETIME()
    WHERE (RunId = @RunKey OR RunId = @RunGuid)
      AND Status NOT IN (N'Canceled', N'Cancelling');
END";

        await using (var command = new SqlCommand(adminSql, connection))
        {
            command.Parameters.Add(new SqlParameter("@WorkItemId", SqlDbType.BigInt) { Value = workItemId });
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<OperationalWorkItemStateTransitionResponse> UpdateAsync(
        long workItemId,
        string setClause,
        string whereClause,
        Action<SqlCommand> configureCommand,
        Func<SqlConnection, long, CancellationToken, Task>? afterUpdateAsync,
        string successStatus,
        string successMessage,
        string notUpdatedMessage,
        CancellationToken cancellationToken)
    {
        if (workItemId <= 0)
        {
            throw new ArgumentException("WorkItemId is required.", nameof(workItemId));
        }

        var schema = GetSchemaName();

        var sql = $"""
            UPDATE [{schema}].[WorkItems]
                SET {setClause}
            OUTPUT
                inserted.WorkItemId,
                inserted.Status,
                inserted.LeaseOwner,
                inserted.StartedAtUtc
            WHERE {whereClause};
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@WorkItemId", SqlDbType.BigInt) { Value = workItemId });

        configureCommand(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return new OperationalWorkItemStateTransitionResponse
            {
                WorkItemId = workItemId,
                Success = false,
                Message = notUpdatedMessage
            };
        }

        var response = new OperationalWorkItemStateTransitionResponse
        {
            WorkItemId = reader.GetInt64(reader.GetOrdinal("WorkItemId")),
            Success = true,
            Message = successMessage,
            Status = successStatus,
            LockedBy = ReadNullableString(reader, "LeaseOwner"),
            LockedAt = ReadNullableDateTimeOffset(reader, "StartedAtUtc")
        };

        await reader.DisposeAsync().ConfigureAwait(false);

        if (afterUpdateAsync is not null)
        {
            await afterUpdateAsync(connection, workItemId, cancellationToken).ConfigureAwait(false);
        }

        return response;
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
}
