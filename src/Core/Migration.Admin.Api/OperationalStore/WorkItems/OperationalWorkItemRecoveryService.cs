using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

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
                Status = N'Created',
                LockedAt = NULL,
                LockedBy = NULL
                """,
            whereClause: """
                WorkItemId = @WorkItemId
                AND Status = N'Locked'
                AND LockedBy = @WorkerId
                """,
            configureCommand: command =>
            {
                command.Parameters.AddWithValue("@WorkerId", request.WorkerId.Trim());
            },
            successStatus: "Created",
            successMessage: "Work item lease released.",
            notUpdatedMessage: "Work item was not released. It may not exist, may not be locked, or may not be locked by the supplied worker.",
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
            "Operational work item {WorkItemId} reset requested. Reason: {Reason}",
            workItemId,
            request.Reason.Trim());

        return UpdateAsync(
            workItemId,
            setClause: """
                Status = N'Created',
                LockedAt = NULL,
                LockedBy = NULL,
                CompletedAt = NULL,
                FailedAt = NULL,
                LastFailureReason = NULL
                """,
            whereClause: "WorkItemId = @WorkItemId",
            configureCommand: _ => { },
            successStatus: "Created",
            successMessage: "Work item reset to Created.",
            notUpdatedMessage: "Work item was not reset. It may not exist.",
            cancellationToken);
    }

    private async Task<OperationalWorkItemStateTransitionResponse> UpdateAsync(
        long workItemId,
        string setClause,
        string whereClause,
        Action<SqlCommand> configureCommand,
        string successStatus,
        string successMessage,
        string notUpdatedMessage,
        CancellationToken cancellationToken)
    {
        if (workItemId < 0)
        {
            throw new ArgumentException("WorkItemId is required.", nameof(workItemId));
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
            WHERE {whereClause};
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@WorkItemId", workItemId);

        configureCommand(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new OperationalWorkItemStateTransitionResponse
            {
                WorkItemId = workItemId,
                Success = false,
                Message = notUpdatedMessage
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
