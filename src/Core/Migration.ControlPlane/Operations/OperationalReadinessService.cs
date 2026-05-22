using Migration.ControlPlane.Audit;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Telemetry;

namespace Migration.ControlPlane.Operations;

public sealed class OperationalReadinessService : IOperationalReadinessService
{
    private readonly IAuditPersistenceProvider _auditProvider;
    private readonly ITelemetrySink _telemetrySink;
    private readonly IQueueExecutionReadinessService _queueReadiness;

    public OperationalReadinessService(
        IAuditPersistenceProvider auditProvider,
        ITelemetrySink telemetrySink,
        IQueueExecutionReadinessService queueReadiness)
    {
        _auditProvider = auditProvider;
        _telemetrySink = telemetrySink;
        _queueReadiness = queueReadiness;
    }

    public OperationalReadinessSnapshot GetSnapshot()
    {
        var blocking = new List<string>();
        var warnings = new List<string>();

        var audit = _auditProvider.Descriptor;
        var telemetry = _telemetrySink.Descriptor;
        var queue = _queueReadiness.GetSnapshot();

        if (!audit.IsConfigured)
        {
            blocking.Add("Audit persistence provider is not configured.");
        }

        if (!telemetry.IsConfigured)
        {
            blocking.Add("Telemetry provider is not configured.");
        }

        if (!audit.IsDurable)
        {
            warnings.Add("Audit persistence provider is not durable.");
        }

        if (!telemetry.IsDurable)
        {
            warnings.Add("Telemetry provider is not durable.");
        }

        foreach (var issue in queue.BlockingIssues)
        {
            AddUnique(blocking, $"queue: {issue}");
        }

        foreach (var warning in audit.Warnings)
        {
            AddUnique(warnings, $"audit: {warning}");
        }

        foreach (var warning in telemetry.Warnings)
        {
            AddUnique(warnings, $"telemetry: {warning}");
        }

        foreach (var warning in queue.Warnings)
        {
            AddUnique(warnings, $"queue: {warning}");
        }

        return new OperationalReadinessSnapshot(
            GeneratedUtc: DateTimeOffset.UtcNow,
            IsOperationallyReady: blocking.Count == 0,
            IsReadyForLiveQueueExecution: queue.IsReadyForLiveExecution && audit.IsDurable && telemetry.IsConfigured,
            Audit: audit,
            Telemetry: telemetry,
            QueueExecution: queue,
            BlockingIssues: blocking,
            Warnings: warnings);
    }

    private static void AddUnique(List<string> values, string value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            !values.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            values.Add(value);
        }
    }
}
