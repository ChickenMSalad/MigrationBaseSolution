namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureDashboardService
    : IOperationalGlobalFailureDashboardService
{
    private readonly IOperationalGlobalFailureService _failureService;
    private readonly IOperationalGlobalFailureMetricsService _metricsService;

    public OperationalGlobalFailureDashboardService(
        IOperationalGlobalFailureService failureService,
        IOperationalGlobalFailureMetricsService metricsService)
    {
        _failureService = failureService;
        _metricsService = metricsService;
    }

    public async Task<OperationalGlobalFailureDashboardResponse> GetDashboardAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeRecentLimit = Math.Clamp(recentLimit, 1, 100);
        var safeMetricsSampleLimit = Math.Clamp(metricsSampleLimit, 1, 500);

        var recentFailures = await _failureService.GetRecentFailuresAsync(
            safeRecentLimit,
            cancellationToken);

        var metrics = await _metricsService.GetMetricsAsync(
            safeMetricsSampleLimit,
            cancellationToken);

        return new OperationalGlobalFailureDashboardResponse
        {
            RecentFailures = recentFailures,
            Metrics = metrics,
            GeneratedAt = DateTimeOffset.UtcNow,
            Messages = BuildMessages(recentFailures, metrics)
        };
    }

    private static IReadOnlyCollection<string> BuildMessages(
        OperationalGlobalRecentFailuresResponse recentFailures,
        OperationalGlobalFailureMetricsResponse metrics)
    {
        var messages = new List<string>
        {
            $"Recent failures returned {recentFailures.Count} failure(s).",
            $"Failure metrics sampled {metrics.TotalFailureCount} failure(s)."
        };

        if (metrics.TotalFailureCount == 0)
        {
            messages.Add("No operational failures are currently visible in the sampled window.");
            return messages;
        }

        messages.Add($"{metrics.RetriableFailureCount} failure(s) are retriable.");
        messages.Add($"{metrics.NonRetriableFailureCount} failure(s) are non-retriable.");

        var topFailureType = metrics.FailureTypes.FirstOrDefault();
        if (topFailureType is not null)
        {
            messages.Add($"Most common failure type is {topFailureType.FailureType}.");
        }

        var topSystemPair = metrics.SystemPairs.FirstOrDefault();
        if (topSystemPair is not null)
        {
            messages.Add($"Most common failure path is {topSystemPair.SourceSystem} to {topSystemPair.TargetSystem}.");
        }

        return messages;
    }
}
