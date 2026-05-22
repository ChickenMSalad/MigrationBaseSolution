namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthRecommendationService
    : IOperationalGlobalRunHealthRecommendationService
{
    private readonly IOperationalGlobalRunHealthDetailedRiskService _detailedRiskService;

    public OperationalGlobalRunHealthRecommendationService(
        IOperationalGlobalRunHealthDetailedRiskService detailedRiskService)
    {
        _detailedRiskService = detailedRiskService;
    }

    public async Task<OperationalGlobalRunHealthRecommendationsResponse> GetRecommendationsAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeRecentLimit = Math.Clamp(recentLimit, 1, 100);
        var safeMetricsSampleLimit = Math.Clamp(metricsSampleLimit, 1, 500);

        var detailedRisk = await _detailedRiskService.GetDetailedRiskAsync(
            safeRecentLimit,
            safeMetricsSampleLimit,
            cancellationToken);

        var recommendations = BuildRecommendations(detailedRisk)
            .OrderBy(r => PriorityRank(r.Priority))
            .ThenBy(r => r.RecommendationKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new OperationalGlobalRunHealthRecommendationsResponse
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            DetailedRisk = detailedRisk,
            RecommendationCount = recommendations.Length,
            Recommendations = recommendations
        };
    }

    private static IEnumerable<OperationalGlobalRunHealthRecommendation> BuildRecommendations(
        OperationalGlobalRunHealthDetailedRiskResponse detailedRisk)
    {
        var bucketsByKey = detailedRisk.Buckets.ToDictionary(
            b => b.BucketKey,
            StringComparer.OrdinalIgnoreCase);

        if (bucketsByKey.TryGetValue("failed-runs", out var failedRuns) &&
            failedRuns.Count > 0)
        {
            yield return new OperationalGlobalRunHealthRecommendation
            {
                RecommendationKey = "review-failed-runs",
                Priority = failedRuns.Count >= 3 ? "High" : "Medium",
                Title = "Review failed runs",
                Description = failedRuns.Message,
                SuggestedAction = "Open failed run projections and determine whether each run should be rerun, finalized as failed, or archived.",
                RelatedSignal = "failed-runs"
            };
        }

        if (bucketsByKey.TryGetValue("failure-records", out var failureRecords) &&
            failureRecords.Count > 0)
        {
            yield return new OperationalGlobalRunHealthRecommendation
            {
                RecommendationKey = "classify-failure-records",
                Priority = failureRecords.Count >= 10 ? "High" : "Medium",
                Title = "Classify failure records",
                Description = failureRecords.Message,
                SuggestedAction = "Use failure analytics to separate retriable failures from non-retriable data/configuration failures.",
                RelatedSignal = "failure-records"
            };
        }

        if (bucketsByKey.TryGetValue("recent-failures", out var recentFailures) &&
            recentFailures.Count > 0)
        {
            yield return new OperationalGlobalRunHealthRecommendation
            {
                RecommendationKey = "inspect-recent-failures",
                Priority = recentFailures.Count >= 10 ? "High" : "Medium",
                Title = "Inspect recent failures",
                Description = recentFailures.Message,
                SuggestedAction = "Compare the recent failure feed with run timelines to identify whether failures are isolated or systemic.",
                RelatedSignal = "recent-failures"
            };
        }

        if (bucketsByKey.TryGetValue("outstanding-work-items", out var outstandingWorkItems) &&
            outstandingWorkItems.Count > 0)
        {
            yield return new OperationalGlobalRunHealthRecommendation
            {
                RecommendationKey = "review-dispatcher-eligibility",
                Priority = outstandingWorkItems.Count >= 100 ? "High" : "Low",
                Title = "Review dispatcher eligibility",
                Description = outstandingWorkItems.Message,
                SuggestedAction = "Check dispatcher diagnostics and queue eligibility to confirm work items can be leased.",
                RelatedSignal = "outstanding-work-items"
            };
        }

        if (bucketsByKey.TryGetValue("locked-work-items", out var lockedWorkItems) &&
            lockedWorkItems.Count > 0)
        {
            yield return new OperationalGlobalRunHealthRecommendation
            {
                RecommendationKey = "verify-active-leases",
                Priority = lockedWorkItems.Count >= 25 ? "Medium" : "Low",
                Title = "Verify active leases",
                Description = lockedWorkItems.Message,
                SuggestedAction = "Check lease heartbeat and expired lease diagnostics for currently locked work items.",
                RelatedSignal = "locked-work-items"
            };
        }

        if (detailedRisk.RiskScore == 0)
        {
            yield return new OperationalGlobalRunHealthRecommendation
            {
                RecommendationKey = "continue-monitoring",
                Priority = "Informational",
                Title = "Continue monitoring",
                Description = "No active operational risk signals are currently present.",
                SuggestedAction = "Continue scheduled smoke tests and operational dashboard monitoring.",
                RelatedSignal = "healthy"
            };
        }
    }

    private static int PriorityRank(string priority)
    {
        return priority switch
        {
            "High" => 0,
            "Medium" => 1,
            "Low" => 2,
            "Informational" => 3,
            _ => 4
        };
    }
}
