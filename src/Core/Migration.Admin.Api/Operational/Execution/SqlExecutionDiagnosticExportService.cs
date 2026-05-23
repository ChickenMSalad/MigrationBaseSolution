using Microsoft.Data.SqlClient;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionDiagnosticExportService : IExecutionDiagnosticExportService
{
    private readonly IConfiguration _configuration;
    private readonly IExecutionPlanStore _planStore;
    private readonly IExecutionWorkItemQueueStore _workItemQueueStore;
    private readonly IExecutionLifecycleService _lifecycleService;
    private readonly IOperationalEventQueryService _eventQueryService;
    private readonly IExecutionWorkerHeartbeatStore _workerHeartbeatStore;

    public SqlExecutionDiagnosticExportService(
        IConfiguration configuration,
        IExecutionPlanStore planStore,
        IExecutionWorkItemQueueStore workItemQueueStore,
        IExecutionLifecycleService lifecycleService,
        IOperationalEventQueryService eventQueryService,
        IExecutionWorkerHeartbeatStore workerHeartbeatStore)
    {
        _configuration = configuration;
        _planStore = planStore;
        _workItemQueueStore = workItemQueueStore;
        _lifecycleService = lifecycleService;
        _eventQueryService = eventQueryService;
        _workerHeartbeatStore = workerHeartbeatStore;
    }

    public async Task<ExecutionDiagnosticExportBundle> BuildBundleAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken)
    {
        var session = await ReadSessionAsync(executionSessionId, cancellationToken);
        var planSteps = await _planStore.ReadPlanAsync(executionSessionId, cancellationToken);
        var workItems = await _workItemQueueStore.ReadRecentAsync(executionSessionId, 1000, cancellationToken);
        var queueSummary = await _workItemQueueStore.ReadSummaryAsync(executionSessionId, cancellationToken);
        var phaseHistory = await _lifecycleService.ReadRecentHistoryAsync(executionSessionId, 250, cancellationToken);
        var operationalEvents = await _eventQueryService.QueryAsync(
            new OperationalEventQueryRequest(
                Severity: null,
                Category: null,
                EventType: null,
                FromUtc: null,
                ToUtc: null,
                ExecutionSessionId: executionSessionId,
                MigrationRunId: null,
                Skip: 0,
                Take: 250),
            cancellationToken);
        var workerTelemetry = await _workerHeartbeatStore.ReadSummaryAsync(
            staleAfterSeconds: 120,
            cancellationToken: cancellationToken);

        return new ExecutionDiagnosticExportBundle(
            GeneratedUtc: DateTimeOffset.UtcNow,
            ExecutionSessionId: executionSessionId,
            Session: session,
            QueueSummary: queueSummary,
            PlanSteps: planSteps,
            WorkItems: workItems,
            PhaseHistory: phaseHistory,
            OperationalEvents: operationalEvents,
            WorkerTelemetry: workerTelemetry);
    }

    private async Task<ExecutionSessionRecord?> ReadSessionAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    ExecutionSessionId,
    MigrationRunId,
    Name,
    SourceConnector,
    TargetConnector,
    Status,
    CreatedUtc,
    StartedUtc,
    CompletedUtc,
    Notes
FROM dbo.MigrationExecutionSessions
WHERE ExecutionSessionId = @ExecutionSessionId;
";

        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ExecutionSessionRecord(
            ExecutionSessionId: reader.GetGuid(0),
            MigrationRunId: reader.IsDBNull(1) ? null : reader.GetGuid(1),
            Name: reader.GetString(2),
            SourceConnector: reader.IsDBNull(3) ? null : reader.GetString(3),
            TargetConnector: reader.IsDBNull(4) ? null : reader.GetString(4),
            Status: reader.GetString(5),
            CreatedUtc: reader.GetFieldValue<DateTimeOffset>(6),
            StartedUtc: reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
            CompletedUtc: reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            Notes: reader.IsDBNull(9) ? null : reader.GetString(9));
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
