using Microsoft.Extensions.Logging;
using Migration.Orchestration.Abstractions;

namespace Migration.Orchestration.Progress;

public sealed class CompositeMigrationProgressReporter : IMigrationProgressReporter
{
    private readonly IReadOnlyList<IMigrationProgressSink> _sinks;
    private readonly ILogger<CompositeMigrationProgressReporter> _logger;

    public CompositeMigrationProgressReporter(
        IEnumerable<IMigrationProgressSink> sinks,
        ILogger<CompositeMigrationProgressReporter> logger)
    {
        _sinks = sinks.ToList();
        _logger = logger;
    }

    public async Task ReportAsync(MigrationProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        foreach (var sink in _sinks)
        {
            try
            {
                await sink.ReportAsync(progressEvent, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Progress publishing should not fail the migration run. Durable execution state remains
                // the source of truth; progress sinks are live notification channels.
                _logger.LogError(ex,
                    "Progress sink {SinkType} failed while publishing event {EventName} for run {RunId}.",
                    sink.GetType().FullName,
                    progressEvent.EventName,
                    progressEvent.RunId);
            }
        }
    }
}

public interface IMigrationProgressSink
{
    Task ReportAsync(MigrationProgressEvent progressEvent, CancellationToken cancellationToken = default);
}
