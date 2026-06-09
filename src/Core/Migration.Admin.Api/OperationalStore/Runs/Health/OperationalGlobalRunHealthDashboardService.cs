namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthDashboardService
    : IOperationalGlobalRunHealthDashboardService
{
    private readonly IOperationalGlobalRunHealthSummaryService _healthSummaryService;
    private readonly IOperationalGlobalActivityDashboardService _activityDashboardService;
    private readonly IOperationalGlobalFailureAnalyticsDashboardService _failureAnalyticsService;

    public OperationalGlobalRunHealthDashboardService(
        IOperationalGlobalRunHealthSummaryService healthSummaryService,
        IOperationalGlobalActivityDashboardService activityDashboardService,
        IOperationalGlobalFailureAnalyticsDashboardService failureAnalyticsService)
    {
        _healthSummaryService = healthSummaryService;
        _activityDashboardService = activityDashboardService;
        _failureAnalyticsService = failureAnalyticsService;
    }

    public async Task<OperationalGlobalRunHealthDashboardResponse> GetDashboardAsync(
        int activityLimit = 10,
        int failureLimit = 10,
        int metricsSampleLimit = 100,
        CancellationToken cancellationToken = default)
    {
        var safeActivityLimit = Math.Clamp(activityLimit, 1, 100);
        var safeFailureLimit = Math.Clamp(failureLimit, 1, 100);
        var safeMetricsSampleLimit = Math.Clamp(metricsSampleLimit, 1, 500);

        var healthSummary = await _healthSummaryService.GetSummaryAsync(cancellationToken);
        var activityDashboard = await _activityDashboardService.GetDashboardAsync(
            safeActivityLimit,
            safeMetricsSampleLimit,
            cancellationToken);
        var failureAnalytics = await _failureAnalyticsService.GetDashboardAsync(
            safeFailureLimit,
            safeMetricsSampleLimit,
            cancellationToken);

        return new OperationalGlobalRunHealthDashboardResponse
        {
            HealthSummary = healthSummary,
            ActivityDashboard = activityDashboard,
            FailureAnalytics = failureAnalytics,
            GeneratedAt = DateTimeOffset.UtcNow,
            Messages = BuildMessages(healthSummary, activityDashboard, failureAnalytics)
        };
    }

    private static IReadOnlyCollection<string> BuildMessages(
        OperationalGlobalRunHealthSummaryResponse healthSummary,
        OperationalGlobalActivityDashboardResponse activityDashboard,
        OperationalGlobalFailureAnalyticsDashboardResponse failureAnalytics)
    {
        var messages = new List<string>();
        messages.AddRange(healthSummary.Messages);
        messages.Add($"Recent activity dashboard returned {activityDashboard.RecentActivity.EventCount} event(s).");
        messages.Add($"Failure analytics dashboard sampled {failureAnalytics.Dashboard.Metrics.TotalFailureCount} failure(s).");

        messages.Add(healthSummary.TotalFailureCount == 0
            ? "No operational failure records are currently present."
            : $"{healthSummary.TotalFailureCount} operational failure record(s) require visibility.");

        return messages;
    }
}


