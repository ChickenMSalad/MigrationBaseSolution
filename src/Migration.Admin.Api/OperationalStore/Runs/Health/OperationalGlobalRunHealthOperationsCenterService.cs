
namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthOperationsCenterService
    : IOperationalGlobalRunHealthOperationsCenterService
{
    private readonly IOperationalGlobalRunHealthDashboardService _dashboardService;
    private readonly IOperationalGlobalRunHealthDetailedRiskService _detailedRiskService;
    private readonly IOperationalGlobalRunHealthRecommendationService _recommendationService;
    private readonly IOperationalGlobalRunHealthActionPlanService _actionPlanService;

    public OperationalGlobalRunHealthOperationsCenterService(
        IOperationalGlobalRunHealthDashboardService dashboardService,
        IOperationalGlobalRunHealthDetailedRiskService detailedRiskService,
        IOperationalGlobalRunHealthRecommendationService recommendationService,
        IOperationalGlobalRunHealthActionPlanService actionPlanService)
    {
        _dashboardService = dashboardService;
        _detailedRiskService = detailedRiskService;
        _recommendationService = recommendationService;
        _actionPlanService = actionPlanService;
    }

    public async Task<OperationalGlobalRunHealthOperationsCenterResponse> GetOperationsCenterAsync(
        int activityRecentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeActivityRecentLimit = Math.Clamp(activityRecentLimit, 1, 100);
        var safeMetricsSampleLimit = Math.Clamp(metricsSampleLimit, 1, 500);

        var dashboard = await _dashboardService.GetDashboardAsync(
            safeActivityRecentLimit,
            safeActivityRecentLimit,
            safeMetricsSampleLimit,
            cancellationToken);

        var detailedRisk = await _detailedRiskService.GetDetailedRiskAsync(
            safeActivityRecentLimit,
            safeMetricsSampleLimit,
            cancellationToken);

        var recommendations = await _recommendationService.GetRecommendationsAsync(
            safeActivityRecentLimit,
            safeMetricsSampleLimit,
            cancellationToken);

        var actionPlan = await _actionPlanService.GetActionPlanAsync(
            safeActivityRecentLimit,
            safeMetricsSampleLimit,
            cancellationToken);

        return new OperationalGlobalRunHealthOperationsCenterResponse
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Dashboard = dashboard,
            DetailedRisk = detailedRisk,
            Recommendations = recommendations,
            ActionPlan = actionPlan,
            OverallPriority = actionPlan.OverallPriority,
            SummaryMessages = BuildSummaryMessages(
                dashboard,
                detailedRisk,
                recommendations,
                actionPlan)
        };
    }

    private static IReadOnlyCollection<string> BuildSummaryMessages(
        OperationalGlobalRunHealthDashboardResponse dashboard,
        OperationalGlobalRunHealthDetailedRiskResponse detailedRisk,
        OperationalGlobalRunHealthRecommendationsResponse recommendations,
        OperationalGlobalRunHealthActionPlanResponse actionPlan)
    {
        var messages = new List<string>
        {
            $"Run health risk level is {detailedRisk.RiskLevel}.",
            $"Run health risk score is {detailedRisk.RiskScore}.",
            $"Overall action priority is {actionPlan.OverallPriority}.",
            $"{recommendations.RecommendationCount} recommendation(s) are available.",
            $"{actionPlan.ActionCount} action plan item(s) are available.",
            $"Operational store contains {dashboard.HealthSummary.TotalRunCount} run(s).",
            $"Failure analytics sampled {dashboard.FailureAnalytics.Dashboard.Metrics.TotalFailureCount} failure(s)."
        };

        if (dashboard.HealthSummary.FailedRunCount > 0)
        {
            messages.Add($"{dashboard.HealthSummary.FailedRunCount} failed run(s) require review.");
        }

        if (dashboard.HealthSummary.OutstandingWorkItemCount > 0)
        {
            messages.Add($"{dashboard.HealthSummary.OutstandingWorkItemCount} outstanding work item(s) remain.");
        }

        if (detailedRisk.RiskScore == 0)
        {
            messages.Add("No active operational risk signals are currently present.");
        }

        return messages;
    }
}
