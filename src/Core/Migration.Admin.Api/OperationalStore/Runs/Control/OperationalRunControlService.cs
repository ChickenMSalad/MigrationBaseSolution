using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunControlService : IOperationalRunControlService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;
    private readonly ILogger<OperationalRunControlService> _logger;

    public OperationalRunControlService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions,
        ILogger<OperationalRunControlService> logger)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
        _logger = logger;
    }

    public async Task<OperationalRunControlStateResponse> GetControlStateAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        var sql = $"""
            SELECT
                r.RunId,
                r.Status,
                ActiveLeaseCount = (
                    SELECT COUNT(1)
                    FROM [{schema}].[WorkItems]
                    WHERE RunId = r.RunId AND Status = N'Locked'
                ),
                OutstandingWorkItemCount = (
                    SELECT COUNT(1)
                    FROM [{schema}].[WorkItems]
                    WHERE RunId = r.RunId AND Status IN (N'Created', N'Locked', N'Processing')
                ),
                CompletedWorkItemCount = (
                    SELECT COUNT(1)
                    FROM [{schema}].[WorkItems]
                    WHERE RunId = r.RunId AND Status = N'Completed'
                ),
                FailedWorkItemCount = (
                    SELECT COUNT(1)
                    FROM [{schema}].[WorkItems]
                    WHERE RunId = r.RunId AND Status = N'Failed'
                )
            FROM [{schema}].[Runs] r
            WHERE r.RunId = @RunId;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new OperationalRunControlStateResponse
            {
                RunId = runId,
                CurrentStatus = "Unknown",
                Message = "Operational run not found."
            };
        }

        var status = reader.GetString(reader.GetOrdinal("Status"));

        return new OperationalRunControlStateResponse
        {
            RunId = runId,
            CurrentStatus = status,
            CancelRequested = status.Equals("CancelRequested", StringComparison.OrdinalIgnoreCase),
            Aborted = status.Equals("Aborted", StringComparison.OrdinalIgnoreCase),
            ActiveLeaseCount = Convert.ToInt32(reader["ActiveLeaseCount"]),
            OutstandingWorkItemCount = Convert.ToInt32(reader["OutstandingWorkItemCount"]),
            CompletedWorkItemCount = Convert.ToInt32(reader["CompletedWorkItemCount"]),
            FailedWorkItemCount = Convert.ToInt32(reader["FailedWorkItemCount"]),
            UpdatedAt = DateTimeOffset.UtcNow,
            Message = "Operational run control state loaded."
        };
    }

    public async Task<OperationalRunControlStateResponse> RequestCancelAsync(
        Guid runId,
        OperationalRunControlActionRequest request,
        CancellationToken cancellationToken = default)
    {
        await UpdateRunStatusAsync(runId, "CancelRequested", cancellationToken);

        _logger.LogWarning(
            "Operational run {RunId} cancel requested by {RequestedBy}. Reason: {Reason}",
            runId,
            request.RequestedBy,
            request.Reason);

        return await GetControlStateAsync(runId, cancellationToken);
    }

    public async Task<OperationalRunControlStateResponse> AbortAsync(
        Guid runId,
        OperationalRunControlActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        var sql = $"""
            UPDATE [{schema}].[Runs]
                SET Status = N'Aborted'
            WHERE RunId = @RunId;

            UPDATE [{schema}].[WorkItems]
                SET Status = N'Created',
                    LockedAt = NULL,
                    LockedBy = NULL
            WHERE RunId = @RunId
              AND Status = N'Locked';
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogError(
            "Operational run {RunId} aborted by {RequestedBy}. Reason: {Reason}",
            runId,
            request.RequestedBy,
            request.Reason);

        return await GetControlStateAsync(runId, cancellationToken);
    }

    public async Task<OperationalRunControlStateResponse> ResumeAsync(
        Guid runId,
        OperationalRunControlActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var state = await GetControlStateAsync(runId, cancellationToken);

        if (state.CurrentStatus == "Unknown")
        {
            return state;
        }

        if (state.Aborted)
        {
            return CopyWithMessage(
                state,
                "Aborted operational runs cannot be resumed. Use explicit reset/recovery tooling if needed.");
        }

        if (!state.CancelRequested)
        {
            return CopyWithMessage(
                state,
                "Operational run is not cancel-requested; resume was not needed.");
        }

        var resumedStatus =
            state.CompletedWorkItemCount > 0 || state.FailedWorkItemCount > 0
                ? "Started"
                : "Created";

        await UpdateRunStatusAsync(runId, resumedStatus, cancellationToken);

        _logger.LogWarning(
            "Operational run {RunId} resumed to {Status} by {RequestedBy}. Reason: {Reason}",
            runId,
            resumedStatus,
            request.RequestedBy,
            request.Reason);

        var updated = await GetControlStateAsync(runId, cancellationToken);

        return CopyWithMessage(updated, $"Operational run resumed to {resumedStatus}.");
    }

    private async Task UpdateRunStatusAsync(
        Guid runId,
        string status,
        CancellationToken cancellationToken)
    {
        var schema = GetSchemaName();

        var sql = $"""
            UPDATE [{schema}].[Runs]
                SET Status = @Status
            WHERE RunId = @RunId;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@Status", status);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName)
            ? "migration"
            : _sqlOptions.Value.SchemaName;
    }

    private static OperationalRunControlStateResponse CopyWithMessage(
        OperationalRunControlStateResponse source,
        string message)
    {
        return new OperationalRunControlStateResponse
        {
            RunId = source.RunId,
            CurrentStatus = source.CurrentStatus,
            CancelRequested = source.CancelRequested,
            Aborted = source.Aborted,
            ActiveLeaseCount = source.ActiveLeaseCount,
            OutstandingWorkItemCount = source.OutstandingWorkItemCount,
            CompletedWorkItemCount = source.CompletedWorkItemCount,
            FailedWorkItemCount = source.FailedWorkItemCount,
            UpdatedAt = source.UpdatedAt,
            Message = message
        };
    }
}


