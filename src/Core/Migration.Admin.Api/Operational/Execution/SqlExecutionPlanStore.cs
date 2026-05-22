using Microsoft.Data.SqlClient;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionPlanStore : IExecutionPlanStore
{
    private readonly IConfiguration _configuration;
    private readonly IOperationalEventStore _eventStore;

    public SqlExecutionPlanStore(
        IConfiguration configuration,
        IOperationalEventStore eventStore)
    {
        _configuration = configuration;
        _eventStore = eventStore;
    }

    public async Task<IReadOnlyList<ExecutionPlanStepRecord>> SeedDefaultPlanAsync(
        SeedExecutionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        Guid? migrationRunId;

        await using (var sessionCommand = connection.CreateCommand())
        {
            sessionCommand.CommandText = @"
SELECT MigrationRunId
FROM dbo.MigrationExecutionSessions
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

        await using (var existsCommand = connection.CreateCommand())
        {
            existsCommand.CommandText = @"
SELECT COUNT(1)
FROM dbo.MigrationExecutionPlanSteps
WHERE ExecutionSessionId = @ExecutionSessionId;
";
            existsCommand.Parameters.AddWithValue("@ExecutionSessionId", request.ExecutionSessionId);

            var existingCount = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken));
            if (existingCount > 0)
            {
                return await ReadPlanAsync(request.ExecutionSessionId, cancellationToken);
            }
        }

        var steps = new[]
        {
            new StepSeed(1, "validation", "Validate execution configuration"),
            new StepSeed(2, "manifest", "Load and validate manifest"),
            new StepSeed(3, "mapping", "Prepare source-target identity mapping"),
            new StepSeed(4, "queue", "Queue migration work items"),
            new StepSeed(5, "execution", "Execute migration work items"),
            new StepSeed(6, "reconciliation", "Reconcile migrated assets"),
            new StepSeed(7, "finalization", "Finalize execution session")
        };

        await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            foreach (var step in steps)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = @"
INSERT INTO dbo.MigrationExecutionPlanSteps
(
    ExecutionPlanStepId,
    ExecutionSessionId,
    MigrationRunId,
    StepOrder,
    StepType,
    StepName,
    Status,
    SourceConnector,
    TargetConnector,
    CreatedUtc
)
VALUES
(
    @ExecutionPlanStepId,
    @ExecutionSessionId,
    @MigrationRunId,
    @StepOrder,
    @StepType,
    @StepName,
    @Status,
    @SourceConnector,
    @TargetConnector,
    SYSUTCDATETIME()
);
";

                command.Parameters.AddWithValue("@ExecutionPlanStepId", Guid.NewGuid());
                command.Parameters.AddWithValue("@ExecutionSessionId", request.ExecutionSessionId);
                command.Parameters.AddWithValue("@MigrationRunId", (object?)migrationRunId ?? DBNull.Value);
                command.Parameters.AddWithValue("@StepOrder", step.StepOrder);
                command.Parameters.AddWithValue("@StepType", step.StepType);
                command.Parameters.AddWithValue("@StepName", step.StepName);
                command.Parameters.AddWithValue("@Status", "pending");
                command.Parameters.AddWithValue("@SourceConnector", NormalizeDbValue(request.SourceConnector));
                command.Parameters.AddWithValue("@TargetConnector", NormalizeDbValue(request.TargetConnector));

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        await _eventStore.WriteAsync(
            eventType: "ExecutionPlanSeeded",
            severity: "info",
            category: "execution",
            source: "Migration.Admin.Api",
            message: $"Execution plan seeded with {steps.Length} step(s).",
            payloadJson: null,
            executionSessionId: request.ExecutionSessionId,
            migrationRunId: migrationRunId,
            cancellationToken: cancellationToken);

        return await ReadPlanAsync(request.ExecutionSessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<ExecutionPlanStepRecord>> ReadPlanAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();
        var steps = new List<ExecutionPlanStepRecord>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    ExecutionPlanStepId,
    ExecutionSessionId,
    MigrationRunId,
    StepOrder,
    StepType,
    StepName,
    Status,
    SourceConnector,
    TargetConnector,
    PayloadJson,
    CreatedUtc,
    StartedUtc,
    CompletedUtc,
    ErrorMessage
FROM dbo.MigrationExecutionPlanSteps
WHERE ExecutionSessionId = @ExecutionSessionId
ORDER BY StepOrder ASC;
";

        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            steps.Add(new ExecutionPlanStepRecord(
                ExecutionPlanStepId: reader.GetGuid(0),
                ExecutionSessionId: reader.GetGuid(1),
                MigrationRunId: reader.IsDBNull(2) ? null : reader.GetGuid(2),
                StepOrder: reader.GetInt32(3),
                StepType: reader.GetString(4),
                StepName: reader.GetString(5),
                Status: reader.GetString(6),
                SourceConnector: reader.IsDBNull(7) ? null : reader.GetString(7),
                TargetConnector: reader.IsDBNull(8) ? null : reader.GetString(8),
                PayloadJson: reader.IsDBNull(9) ? null : reader.GetString(9),
                CreatedUtc: reader.GetFieldValue<DateTimeOffset>(10),
                StartedUtc: reader.IsDBNull(11) ? null : reader.GetFieldValue<DateTimeOffset>(11),
                CompletedUtc: reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTimeOffset>(12),
                ErrorMessage: reader.IsDBNull(13) ? null : reader.GetString(13)));
        }

        return steps;
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
        return string.IsNullOrWhiteSpace(value)
            ? DBNull.Value
            : value.Trim();
    }

    private sealed record StepSeed(
        int StepOrder,
        string StepType,
        string StepName);
}
