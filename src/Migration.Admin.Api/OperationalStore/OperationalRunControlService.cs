using Migration.Infrastructure.State.OperationalStore.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunControlService : IOperationalRunControlService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;

    public OperationalRunControlService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
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
                FROM [{schema}].[MigrationWorkItems]
                WHERE RunId = r.RunId
                  AND Status = N'Locked'
            ),
            OutstandingWorkItemCount = (
                SELECT COUNT(1)
                FROM [{schema}].[MigrationWorkItems]
                WHERE RunId = r.RunId
                  AND Status IN (N'Created', N'Locked', N'Processing')
            ),
            CompletedWorkItemCount = (
                SELECT COUNT(1)
                FROM [{schema}].[MigrationWorkItems]
                WHERE RunId = r.RunId
                  AND Status = N'Completed'
            ),
            FailedWorkItemCount = (
                SELECT COUNT(1)
                FROM [{schema}].[MigrationWorkItems]
                WHERE RunId = r.RunId
                  AND Status = N'Failed'
            )
        FROM [{schema}].[MigrationRuns] r
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
            CancelRequested = status == "CancelRequested",
            Aborted = status == "Aborted",
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

        return await GetControlStateAsync(runId, cancellationToken);
    }

    public async Task<OperationalRunControlStateResponse> AbortAsync(
        Guid runId,
        OperationalRunControlActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        var sql = $"""
        UPDATE [{schema}].[MigrationRuns]
            SET Status = N'Aborted'
        WHERE RunId = @RunId;

        UPDATE [{schema}].[MigrationWorkItems]
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

        return await GetControlStateAsync(runId, cancellationToken);
    }

    private async Task UpdateRunStatusAsync(
        Guid runId,
        string status,
        CancellationToken cancellationToken)
    {
        var schema = GetSchemaName();

        var sql = $"""
        UPDATE [{schema}].[MigrationRuns]
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
}
