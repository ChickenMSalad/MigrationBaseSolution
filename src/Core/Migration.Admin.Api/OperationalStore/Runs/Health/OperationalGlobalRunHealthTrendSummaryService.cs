namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthTrendSummaryService
    : IOperationalGlobalRunHealthTrendSummaryService
{
    private readonly IOperationalGlobalRunHealthSnapshotService _snapshotService;

    public OperationalGlobalRunHealthTrendSummaryService(
        IOperationalGlobalRunHealthSnapshotService snapshotService)
    {
        _snapshotService = snapshotService;
    }

    public async Task<OperationalGlobalRunHealthTrendSummaryResponse> GetTrendSummaryAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeRecentLimit = Math.Clamp(recentLimit, 1, 100);
        var safeMetricsSampleLimit = Math.Clamp(metricsSampleLimit, 1, 500);

        var snapshot = await _snapshotService.GetSnapshotAsync(
            safeRecentLimit,
            safeMetricsSampleLimit,
            cancellationToken);

        var signals = BuildSignals(snapshot).ToArray();
        var weightedRisk = Math.Clamp(
            snapshot.ActiveRiskScore + signals.Sum(s => s.Weight),
            0,
            100);

        return new OperationalGlobalRunHealthTrendSummaryResponse
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            CurrentSnapshot = snapshot,
            TrendDirection = DetermineTrendDirection(snapshot, signals),
            TrendMessage = BuildTrendMessage(snapshot, signals),
            RiskScore = weightedRisk,
            RiskLevel = ToRiskLevel(weightedRisk),
            RecentActivityEventCount = snapshot.RecentActivityEventCount,
            RecentFailureCount = snapshot.RecentFailureCount,
            ActiveRunCount = snapshot.Summary.ActiveRunCount,
            OutstandingWorkItemCount = snapshot.Summary.OutstandingWorkItemCount,
            LockedWorkItemCount = snapshot.Summary.LockedWorkItemCount,
            Signals = signals
        };
    }

    private static IEnumerable<OperationalGlobalRunHealthTrendSignal> BuildSignals(
        OperationalGlobalRunHealthSnapshotResponse snapshot)
    {
        if (snapshot.RecentFailureCount > 0)
        {
            yield return new OperationalGlobalRunHealthTrendSignal
            {
                SignalKey = "recent-failures",
                Severity = snapshot.RecentFailureCount >= 10 ? "High" : "Elevated",
                Message = $"{snapshot.RecentFailureCount} recent failure(s) are present.",
                Weight = Math.Min(snapshot.RecentFailureCount * 5, 25)
            };
        }

        if (snapshot.Summary.FailedRunCount > 0)
        {
            yield return new OperationalGlobalRunHealthTrendSignal
            {
                SignalKey = "failed-runs",
                Severity = snapshot.Summary.FailedRunCount >= 3 ? "High" : "Elevated",
                Message = $"{snapshot.Summary.FailedRunCount} failed run(s) are present.",
                Weight = Math.Min(snapshot.Summary.FailedRunCount * 10, 30)
            };
        }

        if (snapshot.Summary.OutstandingWorkItemCount > 0)
        {
            yield return new OperationalGlobalRunHealthTrendSignal
            {
                SignalKey = "outstanding-work-items",
                Severity = snapshot.Summary.OutstandingWorkItemCount >= 100 ? "High" : "Informational",
                Message = $"{snapshot.Summary.OutstandingWorkItemCount} outstanding work item(s) are present.",
                Weight = Math.Min(snapshot.Summary.OutstandingWorkItemCount, 20)
            };
        }

        if (snapshot.Summary.LockedWorkItemCount > 0)
        {
            yield return new OperationalGlobalRunHealthTrendSignal
            {
                SignalKey = "locked-work-items",
                Severity = snapshot.Summary.LockedWorkItemCount >= 25 ? "Elevated" : "Informational",
                Message = $"{snapshot.Summary.LockedWorkItemCount} locked work item(s) are visible.",
                Weight = Math.Min(snapshot.Summary.LockedWorkItemCount, 15)
            };
        }

        if (snapshot.Summary.CompletionPercent == 100m &&
            snapshot.Summary.TotalWorkItemCount > 0 &&
            snapshot.Summary.TotalFailureCount == 0)
        {
            yield return new OperationalGlobalRunHealthTrendSignal
            {
                SignalKey = "clean-completion",
                Severity = "Positive",
                Message = "All known work items are complete and no failure records are present.",
                Weight = 0
            };
        }

        if (snapshot.RecentActivityEventCount == 0)
        {
            yield return new OperationalGlobalRunHealthTrendSignal
            {
                SignalKey = "no-recent-activity",
                Severity = "Informational",
                Message = "No recent operational activity is visible in the sampled window.",
                Weight = 0
            };
        }
    }

    private static string DetermineTrendDirection(
        OperationalGlobalRunHealthSnapshotResponse snapshot,
        IReadOnlyCollection<OperationalGlobalRunHealthTrendSignal> signals)
    {
        if (snapshot.ActiveRiskScore >= 50 || signals.Any(s => s.Severity == "High"))
        {
            return "Worsening";
        }

        if (snapshot.ActiveRiskScore >= 25 || signals.Any(s => s.Severity == "Elevated"))
        {
            return "Watch";
        }

        if (signals.Any(s => s.SignalKey == "clean-completion"))
        {
            return "Stable";
        }

        return "Neutral";
    }

    private static string BuildTrendMessage(
        OperationalGlobalRunHealthSnapshotResponse snapshot,
        IReadOnlyCollection<OperationalGlobalRunHealthTrendSignal> signals)
    {
        var highSignals = signals.Count(s => s.Severity == "High");
        var elevatedSignals = signals.Count(s => s.Severity == "Elevated");

        if (highSignals > 0)
        {
            return $"Run health requires attention. {highSignals} high-severity signal(s) are present.";
        }

        if (elevatedSignals > 0)
        {
            return $"Run health should be watched. {elevatedSignals} elevated signal(s) are present.";
        }

        if (snapshot.ActiveRiskScore == 0)
        {
            return "Run health is normal for the current sampled state.";
        }

        return "Run health is stable with low active risk.";
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
}
