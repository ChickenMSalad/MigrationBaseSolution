using Microsoft.Data.SqlClient;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionLifecycleService : IExecutionLifecycleService
{
    private readonly IConfiguration _configuration;
    private readonly IOperationalEventStore _eventStore;

    public SqlExecutionLifecycleService(
        IConfiguration configuration,
        IOperationalEventStore eventStore)
    {
        _configuration = configuration;
        _eventStore = eventStore;
    }

    public async Task<ExecutionPhaseHistoryRecord> TransitionAsync(
        TransitionExecutionPhaseRequest request,
        CancellationToken cancellationToken)
    {
        if (!ExecutionPhaseNames.IsKnown(request.NewPhase))
        {
            throw new InvalidOperationException($"Unknown execution phase: {request.NewPhase}");
        }

        var newPhase = ExecutionPhaseNames.Normalize(request.NewPhase);
        var connectionString = GetConnectionString();
        var historyId = Guid.NewGuid();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        Guid? migrationRunId;
        string? previousPhase;

        await using (var currentCommand = connection.CreateCommand())
        {
            currentCommand.CommandText = @"
SELECT TOP (1)
    MigrationRunId,
    Status
FROM dbo.MigrationExecutionSessions
WHERE ExecutionSessionId = @ExecutionSessionId;
";

            currentCommand.Parameters.AddWithValue("@ExecutionSessionId", request.ExecutionSessionId);

            await using var reader = await currentCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException($"Execution session was not found: {request.ExecutionSessionId}");
            }

            migrationRunId = reader.IsDBNull(0) ? null : reader.GetGuid(0);
            previousPhase = reader.GetString(1);
        }

        await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            await using (var historyCommand = connection.CreateCommand())
            {
                historyCommand.Transaction = (SqlTransaction)transaction;
                historyCommand.CommandText = @"
INSERT INTO dbo.MigrationExecutionPhaseHistory
(
    ExecutionPhaseHistoryId,
    ExecutionSessionId,
    MigrationRunId,
    PreviousPhase,
    NewPhase,
    Reason,
    CreatedUtc
)
VALUES
(
    @ExecutionPhaseHistoryId,
    @ExecutionSessionId,
    @MigrationRunId,
    @PreviousPhase,
    @NewPhase,
    @Reason,
    SYSUTCDATETIME()
);
";

                historyCommand.Parameters.AddWithValue("@ExecutionPhaseHistoryId", historyId);
                historyCommand.Parameters.AddWithValue("@ExecutionSessionId", request.ExecutionSessionId);
                historyCommand.Parameters.AddWithValue("@MigrationRunId", (object?)migrationRunId ?? DBNull.Value);
                historyCommand.Parameters.AddWithValue("@PreviousPhase", (object?)previousPhase ?? DBNull.Value);
                historyCommand.Parameters.AddWithValue("@NewPhase", newPhase);
                historyCommand.Parameters.AddWithValue("@Reason", NormalizeDbValue(request.Reason));

                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var sessionCommand = connection.CreateCommand())
            {
                sessionCommand.Transaction = (SqlTransaction)transaction;
                sessionCommand.CommandText = @"
UPDATE dbo.MigrationExecutionSessions
SET
    Status = @Status,
    StartedUtc = CASE
        WHEN @Status = 'running' AND StartedUtc IS NULL THEN SYSUTCDATETIME()
        ELSE StartedUtc
    END,
    CompletedUtc = CASE
        WHEN @Status IN ('completed', 'failed', 'cancelled') THEN SYSUTCDATETIME()
        ELSE CompletedUtc
    END
WHERE ExecutionSessionId = @ExecutionSessionId;
";

                sessionCommand.Parameters.AddWithValue("@ExecutionSessionId", request.ExecutionSessionId);
                sessionCommand.Parameters.AddWithValue("@Status", newPhase);

                await sessionCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        await _eventStore.WriteAsync(
            eventType: "ExecutionPhaseTransitioned",
            severity: newPhase is "failed" or "cancelled" ? "warning" : "info",
            category: "execution",
            source: "Migration.Admin.Api",
            message: $"Execution session transitioned from '{previousPhase}' to '{newPhase}'.",
            payloadJson: null,
            executionSessionId: request.ExecutionSessionId,
            migrationRunId: migrationRunId,
            cancellationToken: cancellationToken);

        return new ExecutionPhaseHistoryRecord(
            ExecutionPhaseHistoryId: historyId,
            ExecutionSessionId: request.ExecutionSessionId,
            MigrationRunId: migrationRunId,
            PreviousPhase: previousPhase,
            NewPhase: newPhase,
            Reason: NormalizeText(request.Reason),
            CreatedUtc: DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<ExecutionPhaseHistoryRecord>> ReadRecentHistoryAsync(
        Guid executionSessionId,
        int take,
        CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(take, 1, 250);
        var connectionString = GetConnectionString();
        var records = new List<ExecutionPhaseHistoryRecord>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (@Take)
    ExecutionPhaseHistoryId,
    ExecutionSessionId,
    MigrationRunId,
    PreviousPhase,
    NewPhase,
    Reason,
    CreatedUtc
FROM dbo.MigrationExecutionPhaseHistory
WHERE ExecutionSessionId = @ExecutionSessionId
ORDER BY CreatedUtc DESC;
";

        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);
        command.Parameters.AddWithValue("@Take", safeTake);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new ExecutionPhaseHistoryRecord(
                ExecutionPhaseHistoryId: reader.GetGuid(0),
                ExecutionSessionId: reader.GetGuid(1),
                MigrationRunId: reader.IsDBNull(2) ? null : reader.GetGuid(2),
                PreviousPhase: reader.IsDBNull(3) ? null : reader.GetString(3),
                NewPhase: reader.GetString(4),
                Reason: reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedUtc: reader.GetFieldValue<DateTimeOffset>(6)));
        }

        return records;
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

    private static object NormalizeDbValue(string? value)
    {
        var normalized = NormalizeText(value);
        return normalized is null ? DBNull.Value : normalized;
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
