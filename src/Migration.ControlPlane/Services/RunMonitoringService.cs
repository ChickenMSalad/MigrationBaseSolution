using Migration.ControlPlane.Models;
using Migration.Orchestration.Abstractions;

namespace Migration.ControlPlane.Services;

public sealed class RunMonitoringService
{
    private readonly IAdminProjectStore _projectStore;
    private readonly IMigrationExecutionStateMaintenance _stateMaintenance;
    private readonly IRunMonitoringStore _monitoringStore;

    public RunMonitoringService(
        IAdminProjectStore projectStore,
        IMigrationExecutionStateMaintenance stateMaintenance,
        IRunMonitoringStore monitoringStore)
    {
        _projectStore = projectStore;
        _stateMaintenance = stateMaintenance;
        _monitoringStore = monitoringStore;
    }

    public async Task<RunSummaryResponse?> GetSummaryAsync(string runId, CancellationToken cancellationToken = default)
    {
        var run = await _projectStore.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
        if (run is null)
        {
            return null;
        }

        var workItems = await GetMatchingWorkItemsAsync(run, cancellationToken).ConfigureAwait(false);
        var statusCounts = workItems
            .GroupBy(x => x.Status, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

        var total = workItems.Count;
        var succeeded = Count(statusCounts, MigrationWorkItemStatuses.Succeeded);
        var dryRunSucceeded = Count(statusCounts, MigrationWorkItemStatuses.DryRunSucceeded);
        var validationFailed = Count(statusCounts, MigrationWorkItemStatuses.ValidationFailed);
        var skipped = Count(statusCounts, MigrationWorkItemStatuses.SkippedAlreadySucceeded);
        var running = Count(statusCounts, MigrationWorkItemStatuses.Running);
        var failed = Count(statusCounts, MigrationWorkItemStatuses.SourceFailed) + Count(statusCounts, MigrationWorkItemStatuses.TargetFailed);
        var terminalOrKnown = succeeded + dryRunSucceeded + validationFailed + skipped + failed;
        var pending = Math.Max(0, total - terminalOrKnown - running);
        var completeUnits = succeeded + dryRunSucceeded + validationFailed + skipped + failed;

        return new RunSummaryResponse
        {
            RunId = run.RunId,
            ProjectId = run.ProjectId,
            JobName = run.JobName,
            Status = run.Status,
            DryRun = run.DryRun,
            CreatedUtc = run.CreatedUtc,
            UpdatedUtc = run.UpdatedUtc,
            StartedUtc = run.StartedUtc,
            CompletedUtc = run.CompletedUtc,
            Message = run.Message,
            TotalWorkItems = total,
            Succeeded = succeeded,
            DryRunSucceeded = dryRunSucceeded,
            Failed = failed,
            ValidationFailed = validationFailed,
            Running = running,
            Skipped = skipped,
            Pending = pending,
            PercentComplete = total == 0 ? null : Math.Round((decimal)completeUnits / total * 100m, 2),
            StatusCounts = statusCounts,
            RecentFailures = workItems
                .Where(IsFailure)
                .OrderByDescending(x => x.UpdatedUtc)
                .Take(25)
                .Select(ToFailureSummary)
                .ToList()
        };
    }

    public async Task<IReadOnlyList<MigrationWorkItemState>?> GetWorkItemsAsync(string runId, CancellationToken cancellationToken = default)
    {
        var run = await _projectStore.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
        return run is null ? null : await GetMatchingWorkItemsAsync(run, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RunFailureSummary>?> GetFailuresAsync(string runId, CancellationToken cancellationToken = default)
    {
        var workItems = await GetWorkItemsAsync(runId, cancellationToken).ConfigureAwait(false);
        return workItems is null
            ? null
            : workItems.Where(IsFailure).OrderByDescending(x => x.UpdatedUtc).Select(ToFailureSummary).ToList();
    }

    public async Task<IReadOnlyList<RunProgressEventRecord>?> GetEventsAsync(string runId, int take = 500, CancellationToken cancellationToken = default)
    {
        var run = await _projectStore.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
        return run is null ? null : await _monitoringStore.ListEventsAsync(runId, take, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<MigrationWorkItemState>> GetMatchingWorkItemsAsync(MigrationRunControlRecord run, CancellationToken cancellationToken)
    {
        var items = await _stateMaintenance.ListWorkItemsAsync(run.JobName, cancellationToken).ConfigureAwait(false);
        return items
            .Where(x => string.Equals(x.RunId, run.RunId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.JobName, run.JobName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.UpdatedUtc)
            .ToList();
    }

    private static bool IsFailure(MigrationWorkItemState item) =>
        string.Equals(item.Status, MigrationWorkItemStatuses.SourceFailed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(item.Status, MigrationWorkItemStatuses.TargetFailed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(item.Status, MigrationWorkItemStatuses.ValidationFailed, StringComparison.OrdinalIgnoreCase) ||
        !string.IsNullOrWhiteSpace(item.LastError);

    private static RunFailureSummary ToFailureSummary(MigrationWorkItemState item) => new()
    {
        WorkItemId = item.WorkItemId,
        SourceAssetId = item.SourceAssetId,
        TargetAssetId = item.TargetAssetId,
        Status = item.Status,
        Message = item.Message,
        LastError = item.LastError,
        UpdatedUtc = item.UpdatedUtc
    };

    private static int Count(IReadOnlyDictionary<string, int> counts, string status) =>
        counts.TryGetValue(status, out var count) ? count : 0;
}
