using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionDiagnosticExportBundle(
    DateTimeOffset GeneratedUtc,
    Guid ExecutionSessionId,
    ExecutionSessionRecord? Session,
    ExecutionWorkItemQueueSummary? QueueSummary,
    IReadOnlyList<ExecutionPlanStepRecord> PlanSteps,
    IReadOnlyList<ExecutionWorkItemRecord> WorkItems,
    IReadOnlyList<ExecutionPhaseHistoryRecord> PhaseHistory,
    IReadOnlyList<OperationalEventRecord> OperationalEvents,
    ExecutionWorkerTelemetrySummary? WorkerTelemetry);

public sealed record ExecutionDiagnosticExportMetadata(
    Guid ExecutionSessionId,
    DateTimeOffset GeneratedUtc,
    int PlanStepCount,
    int WorkItemCount,
    int PhaseHistoryCount,
    int OperationalEventCount);
