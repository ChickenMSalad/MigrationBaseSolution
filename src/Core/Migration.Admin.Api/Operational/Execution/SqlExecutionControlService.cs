using Microsoft.Data.SqlClient;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionControlService : IExecutionControlService
{
    private readonly IConfiguration _configuration;
    private readonly IOperationalEventStore _eventStore;

    public SqlExecutionControlService(
        IConfiguration configuration,
        IOperationalEventStore eventStore)
    {
        _configuration = configuration;
        _eventStore = eventStore;
    }

    public Task PauseAsync(
        PauseExecutionSessionRequest request,
        CancellationToken cancellationToken)
    {
        return SetStatusAsync(
            request.ExecutionSessionId,
            "paused",
            request.Reason,
            "ExecutionSessionPaused",
            "Execution session paused.",
            cancellationToken);
    }

    public Task ResumeAsync(
        ResumeExecutionSessionRequest request,
        CancellationToken cancellationToken)
    {
        return SetStatusAsync(
            request.ExecutionSessionId,
            "queued",
            request.Reason,
            "ExecutionSessionResumed",
            "Execution session resumed and returned to queued state.",
            cancellationToken);
    }

    public async Task CancelAsync(
        CancelExecutionSessionRequest request,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        Guid? migrationRunId = null;
        var cancelledWorkItems = 0;

        await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            await using (var sessionCommand = connection.CreateCommand())
            {
                sessionCommand.Transaction = (SqlTransaction)transaction;
                sessionCommand.CommandText = @"
UPDATE dbo.MigrationExecutionSessions
SET
    Status = 'cancelled',
    CompletedUtc = SYSUTCDATETIME()
OUTPUT inserted.MigrationRunId
WHERE ExecutionSessionId = @ExecutionSessionId;
";

                sessionCommand.Parameters.AddWithValue("@ExecutionSessionId", request.ExecutionSessionId);

                var result = await sessionCommand.ExecuteScalarAsync(cancellationToken);
                if (result is null)
                {
                    throw new InvalidOperationException($"Execution session was not found: {request.ExecutionSessionId}");
                }

                migrationRunId = result == DBNull.Value ? null : (Guid)result;
            }

            await using (var workCommand = connection.CreateCommand())
            {
                workCommand.Transaction = (SqlTransaction)transaction;
                workCommand.CommandText = @"
UPDATE dbo.MigrationExecutionWorkItems
SET
    Status = 'cancelled',
    WorkerId = NULL,
    LeaseId = NULL,
    LeaseExpiresUtc = NULL,
    CompletedUtc = SYSUTCDATETIME(),
    ErrorMessage = @Reason
WHERE ExecutionSessionId = @ExecutionSessionId
  AND Status IN ('pending', 'leased', 'running', 'failed');
";

                workCommand.Parameters.AddWithValue("@ExecutionSessionId", request.ExecutionSessionId);
                workCommand.Parameters.AddWithValue("@Reason", string.IsNullOrWhiteSpace(request.Reason)
                    ? "Execution session cancelled."
                    : request.Reason.Trim());

                cancelledWorkItems = await workCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        var message = string.IsNullOrWhiteSpace(request.Reason)
            ? $"Execution session cancelled. {cancelledWorkItems} work item(s) cancelled."
            : $"Execution session cancelled. {cancelledWorkItems} work item(s) cancelled. Reason: {request.Reason.Trim()}";

        await _eventStore.WriteAsync(
            eventType: "ExecutionSessionCancelled",
            severity: "warning",
            category: "execution",
            source: "Migration.Admin.Api",
            message: message,
            payloadJson: null,
            executionSessionId: request.ExecutionSessionId,
            migrationRunId: migrationRunId,
            cancellationToken: cancellationToken);
    }

    private async Task SetStatusAsync(
        Guid executionSessionId,
        string status,
        string? reason,
        string eventType,
        string message,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        Guid? migrationRunId = null;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
UPDATE dbo.MigrationExecutionSessions
SET Status = @Status
OUTPUT inserted.MigrationRunId
WHERE ExecutionSessionId = @ExecutionSessionId;
";

            command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);
            command.Parameters.AddWithValue("@Status", status);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException($"Execution session was not found: {executionSessionId}");
            }

            migrationRunId = result == DBNull.Value ? null : (Guid)result;
        }

        await _eventStore.WriteAsync(
            eventType: eventType,
            severity: "info",
            category: "execution",
            source: "Migration.Admin.Api",
            message: string.IsNullOrWhiteSpace(reason) ? message : $"{message} Reason: {reason.Trim()}",
            payloadJson: null,
            executionSessionId: executionSessionId,
            migrationRunId: migrationRunId,
            cancellationToken: cancellationToken);
    }

    private string GetConnectionString()
    {
        var connectionString =
            _configuration.GetConnectionString("OperationalSql") ??
            _configuration["OperationalSql:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Operational SQL connection string is not configured.");
        }

        return connectionString;
    }
}
