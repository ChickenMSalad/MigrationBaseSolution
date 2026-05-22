namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthDetailedRiskService
    : IOperationalGlobalRunHealthDetailedRiskService
{
    private readonly IOperationalGlobalRunHealthTrendSummaryService _trendSummaryService;

    public OperationalGlobalRunHealthDetailedRiskService(
        IOperationalGlobalRunHealthTrendSummaryService trendSummaryService)
    {
        _trendSummaryService = trendSummaryService;
    }

    public async Task<OperationalGlobalRunHealthDetailedRiskResponse> GetDetailedRiskAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeRecentLimit = Math.Clamp(recentLimit, 1, 100);
        var safeMetricsSampleLimit = Math.Clamp(metricsSampleLimit, 1, 500);

        var trend = await _trendSummaryService.GetTrendSummaryAsync(
            safeRecentLimit,
            safeMetricsSampleLimit,
            cancellationToken);

        var buckets = BuildBuckets(trend).ToArray();
        var bucketScore = buckets.Sum(b => b.ScoreContribution);
        var score = Math.Clamp(Math.Max(trend.RiskScore, bucketScore), 0, 100);

        return new OperationalGlobalRunHealthDetailedRiskResponse
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            TrendSummary = trend,
            RiskScore = score,
            RiskLevel = ToRiskLevel(score),
            RiskPosture = ToRiskPosture(score, buckets),
            Buckets = buckets,
            Recommendations = BuildRecommendations(score, buckets)
        };
    }

    private static IEnumerable<OperationalGlobalRunHealthRiskBucket> BuildBuckets(
        OperationalGlobalRunHealthTrendSummaryResponse trend)
    {
        var summary = trend.CurrentSnapshot.Summary;

        yield return new OperationalGlobalRunHealthRiskBucket
        {
            BucketKey = "failed-runs",
            DisplayName = "Failed runs",
            Severity = summary.FailedRunCount > 0 ? "High" : "Normal",
            ScoreContribution = Math.Min(summary.FailedRunCount * 25, 50),
            Count = summary.FailedRunCount,
            Message = summary.FailedRunCount > 0
                ? $"{summary.FailedRunCount} failed run(s) are present."
                : "No failed runs are currently present."
        };

        yield return new OperationalGlobalRunHealthRiskBucket
        {
            BucketKey = "failure-records",
            DisplayName = "Failure records",
            Severity = summary.TotalFailureCount > 0 ? "Elevated" : "Normal",
            ScoreContribution = Math.Min(summary.TotalFailureCount * 10, 30),
            Count = summary.TotalFailureCount,
            Message = summary.TotalFailureCount > 0
                ? $"{summary.TotalFailureCount} failure record(s) are present."
                : "No failure records are currently present."
        };

        yield return new OperationalGlobalRunHealthRiskBucket
        {
            BucketKey = "outstanding-work-items",
            DisplayName = "Outstanding work items",
            Severity = summary.OutstandingWorkItemCount > 0 ? "Watch" : "Normal",
            ScoreContribution = Math.Min(summary.OutstandingWorkItemCount * 2, 20),
            Count = summary.OutstandingWorkItemCount,
            Message = summary.OutstandingWorkItemCount > 0
                ? $"{summary.OutstandingWorkItemCount} outstanding work item(s) are present."
                : "No outstanding work items are currently present."
        };

        yield return new OperationalGlobalRunHealthRiskBucket
        {
            BucketKey = "locked-work-items",
            DisplayName = "Locked work items",
            Severity = summary.LockedWorkItemCount > 0 ? "Watch" : "Normal",
            ScoreContribution = Math.Min(summary.LockedWorkItemCount * 2, 15),
            Count = summary.LockedWorkItemCount,
            Message = summary.LockedWorkItemCount > 0
                ? $"{summary.LockedWorkItemCount} locked work item(s) are visible."
                : "No locked work items are currently visible."
        };

        yield return new OperationalGlobalRunHealthRiskBucket
        {
            BucketKey = "recent-failures",
            DisplayName = "Recent failures",
            Severity = trend.RecentFailureCount > 0 ? "Elevated" : "Normal",
            ScoreContribution = Math.Min(trend.RecentFailureCount * 10, 30),
            Count = trend.RecentFailureCount,
            Message = trend.RecentFailureCount > 0
                ? $"{trend.RecentFailureCount} recent failure(s) are visible in the sampled window."
                : "No recent failures are visible in the sampled window."
        };
    }

    private static IReadOnlyCollection<string> BuildRecommendations(
        int score,
        IReadOnlyCollection<OperationalGlobalRunHealthRiskBucket> buckets)
    {
        var recommendations = new List<string>();

        if (score == 0)
        {
            recommendations.Add("No immediate operational action is required.");
        }

        foreach (var bucket in buckets.Where(b => b.Count > 0))
        {
            recommendations.Add(bucket.BucketKey switch
            {
                "failed-runs" => "Review failed runs and confirm whether rerun or failure finalization is required.",
                "failure-records" => "Review recent failure analytics and classify retriable versus non-retriable failures.",
                "outstanding-work-items" => "Review queue depth and dispatcher eligibility for outstanding work items.",
                "locked-work-items" => "Check dispatcher leases and confirm locked work items are actively heartbeating.",
                "recent-failures" => "Inspect recent failure feed and compare against run timeline events.",
                _ => $"Review {bucket.DisplayName}."
            });
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Continue monitoring operational run health.");
        }

        return recommendations
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

    private static string ToRiskPosture(
        int score,
        IReadOnlyCollection<OperationalGlobalRunHealthRiskBucket> buckets)
    {
        if (score >= 75)
        {
            return "Immediate intervention recommended.";
        }

        if (score >= 50)
        {
            return "Operational review recommended.";
        }

        if (score >= 25)
        {
            return "Monitor closely.";
        }

        if (buckets.Any(b => b.Count > 0))
        {
            return "Low risk with informational signals.";
        }

        return "Healthy.";
    }
}
