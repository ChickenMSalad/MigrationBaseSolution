using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Services;

public interface IRunMonitoringStore
{
    Task SaveEventAsync(RunProgressEventRecord progressEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RunProgressEventRecord>> ListEventsAsync(string runId, int take = 500, CancellationToken cancellationToken = default);
    Task DeleteEventsAsync(string runId, CancellationToken cancellationToken = default);
}
