using Migration.Orchestration.Abstractions;

namespace Migration.Orchestration.Progress;

public sealed class ConsoleMigrationProgressSink : IMigrationProgressSink
{
    public Task ReportAsync(MigrationProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        var count = progressEvent.Completed.HasValue && progressEvent.Total.HasValue
            ? $" [{progressEvent.Completed}/{progressEvent.Total}]"
            : string.Empty;

        var item = string.IsNullOrWhiteSpace(progressEvent.WorkItemId)
            ? string.Empty
            : $" {progressEvent.WorkItemId}";

        Console.WriteLine($"{progressEvent.TimestampUtc:O} {progressEvent.EventName}{count}{item} {progressEvent.Message}".TrimEnd());
        return Task.CompletedTask;
    }
}
