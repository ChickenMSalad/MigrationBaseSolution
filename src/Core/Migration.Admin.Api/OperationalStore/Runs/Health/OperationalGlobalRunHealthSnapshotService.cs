namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthSnapshotService
    : IOperationalGlobalRunHealthSnapshotService
{
    private readonly IOperationalGlobalRunHealthSummaryService _summaryService;
    private readonly IOperationalGlobalActivityFeedService _activityFeedService;
    private readonly IOperationalGlobalFailureDashboardService _failureDashboardService;

    public OperationalGlobalRunHealthSnapshotService(
        IOperationalGlobalRunHealthSummaryService summaryService,
        IOperationalGlobalActivityFeedService activityFeedService,
        IOperationalGlobalFailureDashboardService failureDashboardService)
    {
        _summaryService = summaryService;
        _activityFeedService = activityFeedService;
        _failureDashboardService = failureDashboardService;
    }

    public async Task<OperationalGlobalRunHealthSnapshotResponse> GetSnapshotAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeRecentLimit = Math.Clamp(recentLimit, 1, 100);
        var safeMetricsSampleLimit = Math.Clamp(metricsSampleLimit, 1, 500);

        var summary = await _summaryService.GetSummaryAsync(cancellationToken);

        var activity = await _activityFeedService.GetRecentActivityAsync(
            safeRecentLimit,
            cancellationToken);

        var failures = await _failureDashboardService.GetDashboardAsync(
            safeRecentLimit,
            safeMetricsSampleLimit,
            cancellationToken);

        var riskScore = CalculateRiskScore(
            summary,
            failures);

        return new OperationalGlobalRunHealthSnapshotResponse
        {
            SnapshotAt = DateTimeOffset.UtcNow,
            Summary = summary,
            RecentActivityEventCount = activity.EventCount,
            RecentFailureCount = failures.RecentFailures.Count,
            FailureTypeCount = failures.Metrics.FailureTypes.Count,
            ActiveRiskScore = riskScore,
            RiskLevel = ToRiskLevel(riskScore),
            Signals = BuildSignals(summary, activity.EventCount, failures, riskScore)
        };
    }

    private static int CalculateRiskScore(
        OperationalGlobalRunHealthSummaryResponse summary,
        OperationalGlobalFailureDashboardResponse failures)
    {
        var score = 0;

        score += summary.FailedRunCount * 25;
        score += summary.TotalFailureCount * 10;
        score += failures.RecentFailures.Count * 10;
        score += summary.FailedWorkItemCount * 10;
        score += summary.OutstandingWorkItemCount * 2;
        score += summary.LockedWorkItemCount * 2;

        if (summary.ActiveRunCount > 0)
        {
            score += 5;
        }

        if (summary.CompletionPercent < 100m && summary.TotalWorkItemCount > 0)
        {
            score += 5;
        }

        return Math.Clamp(score, 0, 100);
    }

    private static string ToRiskLevel(int score)
    {
        if (score >= 75)
        {
            return "Critical";
        }

        if (score >= 50)
        {
            return "High";
        }

        if (score >= 25)
        {
            return "Elevated";
        }

        return "Normal";
    }

    private static IReadOnlyCollection<string> BuildSignals(
        OperationalGlobalRunHealthSummaryResponse summary,
        int recentActivityEventCount,
        OperationalGlobalFailureDashboardResponse failures,
        int riskScore)
    {
        var signals = new List<string>
        {
            $"Risk score is {riskScore}.",
            $"Risk level is {ToRiskLevel(riskScore)}.",
            $"Recent activity includes {recentActivityEventCount} event(s).",
            $"Recent failures include {failures.RecentFailures.Count} failure(s)."
        };

        if (summary.FailedRunCount > 0)
        {
            signals.Add($"{summary.FailedRunCount} failed run(s) are present.");
        }

        if (summary.OutstandingWorkItemCount > 0)
        {
            signals.Add($"{summary.OutstandingWorkItemCount} outstanding work item(s) are present.");
        }

        if (summary.LockedWorkItemCount > 0)
        {
            signals.Add($"{summary.LockedWorkItemCount} locked work item(s) are present.");
        }

        if (summary.TotalFailureCount == 0)
        {
            signals.Add("No failure records are currently present.");
        }

        return signals;
    }
}
