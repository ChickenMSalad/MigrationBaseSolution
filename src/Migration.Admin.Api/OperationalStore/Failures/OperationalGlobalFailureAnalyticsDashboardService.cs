namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsDashboardService
    : IOperationalGlobalFailureAnalyticsDashboardService
{
    private readonly IOperationalGlobalFailureDashboardService _dashboardService;
    private readonly IOperationalGlobalFailureSystemPairMetricsService _systemPairMetricsService;
    private readonly IOperationalGlobalFailureRunStatusMetricsService _runStatusMetricsService;
    private readonly IOperationalGlobalFailureCatalogService _catalogService;

    public OperationalGlobalFailureAnalyticsDashboardService(
        IOperationalGlobalFailureDashboardService dashboardService,
        IOperationalGlobalFailureSystemPairMetricsService systemPairMetricsService,
        IOperationalGlobalFailureRunStatusMetricsService runStatusMetricsService,
        IOperationalGlobalFailureCatalogService catalogService)
    {
        _dashboardService = dashboardService;
        _systemPairMetricsService = systemPairMetricsService;
        _runStatusMetricsService = runStatusMetricsService;
        _catalogService = catalogService;
    }

    public async Task<OperationalGlobalFailureAnalyticsDashboardResponse> GetDashboardAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeRecentLimit = Math.Clamp(recentLimit, 1, 100);
        var safeMetricsSampleLimit = Math.Clamp(metricsSampleLimit, 1, 500);

        var dashboard = await _dashboardService.GetDashboardAsync(
            safeRecentLimit,
            safeMetricsSampleLimit,
            cancellationToken);

        var systemPairMetrics = await _systemPairMetricsService.GetMetricsAsync(
            safeMetricsSampleLimit,
            cancellationToken);

        var runStatusMetrics = await _runStatusMetricsService.GetMetricsAsync(
            safeMetricsSampleLimit,
            cancellationToken);

        var catalog = await _catalogService.GetCatalogAsync(
            safeMetricsSampleLimit,
            cancellationToken);

        return new OperationalGlobalFailureAnalyticsDashboardResponse
        {
            Dashboard = dashboard,
            SystemPairMetrics = systemPairMetrics,
            RunStatusMetrics = runStatusMetrics,
            Catalog = catalog,
            GeneratedAt = DateTimeOffset.UtcNow,
            Messages = BuildMessages(
                dashboard,
                systemPairMetrics,
                runStatusMetrics,
                catalog)
        };
    }

    private static IReadOnlyCollection<string> BuildMessages(
        OperationalGlobalFailureDashboardResponse dashboard,
        OperationalGlobalFailureSystemPairMetricsResponse systemPairMetrics,
        OperationalGlobalFailureRunStatusMetricsResponse runStatusMetrics,
        OperationalGlobalFailureCatalogResponse catalog)
    {
        var messages = new List<string>();

        messages.AddRange(dashboard.Messages);

        messages.Add(
            $"Failure analytics include {systemPairMetrics.SystemPairCount} source/target system pair(s).");

        messages.Add(
            $"Failure analytics include {runStatusMetrics.RunStatusCount} run status bucket(s).");

        messages.Add(
            $"Failure catalog includes {catalog.FailureTypeCount} failure type(s).");

        var topSystemPair = systemPairMetrics.SystemPairs.FirstOrDefault();
        if (topSystemPair is not null)
        {
            messages.Add(
                $"Top failure system pair is {topSystemPair.SourceSystem} to {topSystemPair.TargetSystem}.");
        }

        var topRunStatus = runStatusMetrics.RunStatuses.FirstOrDefault();
        if (topRunStatus is not null)
        {
            messages.Add(
                $"Top failure run status is {topRunStatus.RunStatus}.");
        }

        return messages;
    }
}
