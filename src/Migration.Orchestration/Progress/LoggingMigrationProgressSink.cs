using Microsoft.Extensions.Logging;
using Migration.Orchestration.Abstractions;

namespace Migration.Orchestration.Progress;

public sealed class LoggingMigrationProgressSink : IMigrationProgressSink
{
    private readonly ILogger<LoggingMigrationProgressSink> _logger;

    public LoggingMigrationProgressSink(ILogger<LoggingMigrationProgressSink> logger)
    {
        _logger = logger;
    }

    public Task ReportAsync(MigrationProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["RunId"] = progressEvent.RunId,
            ["JobName"] = progressEvent.JobName,
            ["WorkItemId"] = progressEvent.WorkItemId
        });

        _logger.LogInformation("Migration event {EventName}. Completed={Completed} Total={Total} Message={Message}",
            progressEvent.EventName,
            progressEvent.Completed,
            progressEvent.Total,
            progressEvent.Message);

        return Task.CompletedTask;
    }
}
