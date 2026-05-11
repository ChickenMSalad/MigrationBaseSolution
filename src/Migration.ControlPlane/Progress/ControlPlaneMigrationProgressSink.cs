using Migration.ControlPlane.Models;
using Migration.ControlPlane.Services;
using Migration.Orchestration.Abstractions;
using Migration.Orchestration.Progress;

namespace Migration.ControlPlane.Progress;

/// <summary>
/// Persists progress events into the control-plane store so the Admin API can expose
/// /api/runs/{runId}/events without relying on console output or queue inspection.
/// </summary>
public sealed class ControlPlaneMigrationProgressSink : IMigrationProgressSink
{
    private readonly IRunMonitoringStore _store;

    public ControlPlaneMigrationProgressSink(IRunMonitoringStore store)
    {
        _store = store;
    }

    public Task ReportAsync(MigrationProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(progressEvent);

        var record = new RunProgressEventRecord
        {
            EventId = Guid.NewGuid().ToString("N"),
            RunId = progressEvent.RunId,
            JobName = progressEvent.JobName,
            EventName = progressEvent.EventName,
            WorkItemId = progressEvent.WorkItemId,
            Completed = progressEvent.Completed,
            Total = progressEvent.Total,
            Message = progressEvent.Message,
            TimestampUtc = progressEvent.TimestampUtc,
            Properties = progressEvent.Properties is null
                ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string?>(progressEvent.Properties, StringComparer.OrdinalIgnoreCase)
        };

        return _store.SaveEventAsync(record, cancellationToken);
    }
}
