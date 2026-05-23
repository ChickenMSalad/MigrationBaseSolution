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
