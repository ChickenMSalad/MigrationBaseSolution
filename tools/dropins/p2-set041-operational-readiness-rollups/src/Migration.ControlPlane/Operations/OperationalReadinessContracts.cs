using Migration.ControlPlane.Audit;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Telemetry;

namespace Migration.ControlPlane.Operations;

public sealed record OperationalReadinessSnapshot(
    DateTimeOffset GeneratedUtc,
    bool IsOperationallyReady,
    bool IsReadyForLiveQueueExecution,
    AuditPersistenceProviderDescriptor Audit,
    TelemetryProviderDescriptor Telemetry,
    QueueExecutionReadinessSnapshot QueueExecution,
    IReadOnlyList<string> BlockingIssues,
    IReadOnlyList<string> Warnings);
