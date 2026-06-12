namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalActivityDashboardService
    : IOperationalGlobalActivityDashboardService
{
    private readonly IOperationalGlobalActivityFeedService _feedService;
    private readonly IOperationalGlobalActivityMetricsService _metricsService;
    private readonly IOperationalRunTimelineGlobalCatalogService _catalogService;

    public OperationalGlobalActivityDashboardService(
        IOperationalGlobalActivityFeedService feedService,
        IOperationalGlobalActivityMetricsService metricsService,
        IOperationalRunTimelineGlobalCatalogService catalogService)
    {
        _feedService = feedService;
        _metricsService = metricsService;
        _catalogService = catalogService;
    }

    public async Task<OperationalGlobalActivityDashboardResponse> GetDashboardAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeRecentLimit = Math.Clamp(recentLimit, 1, 100);
        var safeMetricsSampleLimit = Math.Clamp(metricsSampleLimit, 1, 500);

        var recentActivity = await _feedService.GetRecentActivityAsync(
            safeRecentLimit,
            cancellationToken);

        var metrics = await _metricsService.GetMetricsAsync(
            safeMetricsSampleLimit,
            cancellationToken);

        var catalog = await _catalogService.GetCatalogAsync(
            cancellationToken);

        return new OperationalGlobalActivityDashboardResponse
        {
            RecentActivity = recentActivity,
            Metrics = metrics,
            Catalog = catalog,
            GeneratedAt = DateTimeOffset.UtcNow,
            Messages = BuildMessages(recentActivity, metrics, catalog)
        };
    }

    private static IReadOnlyCollection<string> BuildMessages(
        OperationalGlobalActivityFeedResponse recentActivity,
        OperationalGlobalActivityMetricsResponse metrics,
        OperationalRunTimelineGlobalCatalogResponse catalog)
    {
        var messages = new List<string>
        {
            $"Recent activity returned {recentActivity.EventCount} event(s).",
            $"Activity metrics sampled {metrics.TotalEventCount} event(s).",
            $"Timeline catalog contains {catalog.EventTypeCount} event type(s) and {catalog.SourceCount} source(s)."
        };

        var topEventType = metrics.EventTypes.FirstOrDefault();
        if (topEventType is not null)
        {
            messages.Add($"Most common recent event type is {topEventType.EventType}.");
        }

        var topSource = metrics.Sources.FirstOrDefault();
        if (topSource is not null)
        {
            messages.Add($"Most active recent source is {topSource.Source}.");
        }

        return messages;
    }
}


