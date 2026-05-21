namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthActionPlanService
    : IOperationalGlobalRunHealthActionPlanService
{
    private readonly IOperationalGlobalRunHealthRecommendationService _recommendationService;

    public OperationalGlobalRunHealthActionPlanService(
        IOperationalGlobalRunHealthRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<OperationalGlobalRunHealthActionPlanResponse> GetActionPlanAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeRecentLimit = Math.Clamp(recentLimit, 1, 100);
        var safeMetricsSampleLimit = Math.Clamp(metricsSampleLimit, 1, 500);

        var recommendations = await _recommendationService.GetRecommendationsAsync(
            safeRecentLimit,
            safeMetricsSampleLimit,
            cancellationToken);

        var actions = recommendations.Recommendations
            .Select(ToAction)
            .OrderBy(a => PriorityRank(a.Priority))
            .ThenBy(a => a.Sequence)
            .ToArray();

        actions = actions
            .Select((a, index) => new OperationalGlobalRunHealthActionPlanItem
            {
                Sequence = index + 1,
                ActionKey = a.ActionKey,
                Category = a.Category,
                Priority = a.Priority,
                OwnerHint = a.OwnerHint,
                Title = a.Title,
                Action = a.Action,
                ValidationHint = a.ValidationHint,
                SourceRecommendationKey = a.SourceRecommendationKey
            })
            .ToArray();

        return new OperationalGlobalRunHealthActionPlanResponse
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Recommendations = recommendations,
            ActionCount = actions.Length,
            OverallPriority = DetermineOverallPriority(actions),
            Actions = actions
        };
    }

    private static OperationalGlobalRunHealthActionPlanItem ToAction(
        OperationalGlobalRunHealthRecommendation recommendation)
    {
        return recommendation.RecommendationKey switch
        {
            "review-failed-runs" => new OperationalGlobalRunHealthActionPlanItem
            {
                Sequence = 10,
                ActionKey = "action-review-failed-runs",
                Category = "Run Recovery",
                Priority = recommendation.Priority,
                OwnerHint = "Migration operator",
                Title = "Review failed runs",
                Action = recommendation.SuggestedAction,
                ValidationHint = "Confirm failed runs have an explicit rerun, finalize-failure, or archive decision.",
                SourceRecommendationKey = recommendation.RecommendationKey
            },
            "classify-failure-records" => new OperationalGlobalRunHealthActionPlanItem
            {
                Sequence = 20,
                ActionKey = "action-classify-failures",
                Category = "Failure Triage",
                Priority = recommendation.Priority,
                OwnerHint = "Migration support",
                Title = "Classify failure records",
                Action = recommendation.SuggestedAction,
                ValidationHint = "Confirm failure analytics separates retriable from non-retriable failures.",
                SourceRecommendationKey = recommendation.RecommendationKey
            },
            "inspect-recent-failures" => new OperationalGlobalRunHealthActionPlanItem
            {
                Sequence = 30,
                ActionKey = "action-inspect-recent-failures",
                Category = "Failure Triage",
                Priority = recommendation.Priority,
                OwnerHint = "Migration support",
                Title = "Inspect recent failures",
                Action = recommendation.SuggestedAction,
                ValidationHint = "Confirm recent failures have matching timeline events.",
                SourceRecommendationKey = recommendation.RecommendationKey
            },
            "review-dispatcher-eligibility" => new OperationalGlobalRunHealthActionPlanItem
            {
                Sequence = 40,
                ActionKey = "action-review-dispatcher-eligibility",
                Category = "Dispatcher",
                Priority = recommendation.Priority,
                OwnerHint = "Platform operator",
                Title = "Review dispatcher eligibility",
                Action = recommendation.SuggestedAction,
                ValidationHint = "Confirm dispatcher diagnostics show expected eligible and blocked counts.",
                SourceRecommendationKey = recommendation.RecommendationKey
            },
            "verify-active-leases" => new OperationalGlobalRunHealthActionPlanItem
            {
                Sequence = 50,
                ActionKey = "action-verify-active-leases",
                Category = "Dispatcher",
                Priority = recommendation.Priority,
                OwnerHint = "Platform operator",
                Title = "Verify active leases",
                Action = recommendation.SuggestedAction,
                ValidationHint = "Confirm locked work items are actively heartbeating or reclaimed.",
                SourceRecommendationKey = recommendation.RecommendationKey
            },
            "continue-monitoring" => new OperationalGlobalRunHealthActionPlanItem
            {
                Sequence = 100,
                ActionKey = "action-continue-monitoring",
                Category = "Monitoring",
                Priority = recommendation.Priority,
                OwnerHint = "Migration operator",
                Title = "Continue monitoring",
                Action = recommendation.SuggestedAction,
                ValidationHint = "Continue scheduled smoke tests and dashboard review.",
                SourceRecommendationKey = recommendation.RecommendationKey
            },
            _ => new OperationalGlobalRunHealthActionPlanItem
            {
                Sequence = 90,
                ActionKey = $"action-{recommendation.RecommendationKey}",
                Category = "General",
                Priority = recommendation.Priority,
                OwnerHint = "Migration operator",
                Title = recommendation.Title,
                Action = recommendation.SuggestedAction,
                ValidationHint = "Validate the recommendation source signal has been reviewed.",
                SourceRecommendationKey = recommendation.RecommendationKey
            }
        };
    }

    private static string DetermineOverallPriority(
        IReadOnlyCollection<OperationalGlobalRunHealthActionPlanItem> actions)
    {
        if (actions.Any(a => a.Priority == "High"))
        {
            return "High";
        }

        if (actions.Any(a => a.Priority == "Medium"))
        {
            return "Medium";
        }

        if (actions.Any(a => a.Priority == "Low"))
        {
            return "Low";
        }

        return "Informational";
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
