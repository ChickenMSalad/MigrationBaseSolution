namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalActivityMetricsService
    : IOperationalGlobalActivityMetricsService
{
    private readonly IOperationalGlobalActivityFeedService _activityFeedService;

    public OperationalGlobalActivityMetricsService(
        IOperationalGlobalActivityFeedService activityFeedService)
    {
        _activityFeedService = activityFeedService;
    }

    public async Task<OperationalGlobalActivityMetricsResponse> GetMetricsAsync(
        int sampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(sampleLimit, 1, 500);

        var feed = await _activityFeedService.GetRecentActivityAsync(
            safeLimit,
            cancellationToken);

        var events = feed.Events.ToArray();

        return new OperationalGlobalActivityMetricsResponse
        {
            TotalEventCount = events.Length,
            FirstEventAt = events.Length == 0 ? null : events.Min(e => e.OccurredAt),
            LastEventAt = events.Length == 0 ? null : events.Max(e => e.OccurredAt),
            GeneratedAt = DateTimeOffset.UtcNow,
            EventTypes = events
                .GroupBy(e => e.EventType, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new OperationalGlobalActivityEventTypeMetric
                {
                    EventType = g.Key,
                    Count = g.Count()
                })
                .ToArray(),
            Sources = events
                .GroupBy(e => e.Source, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new OperationalGlobalActivitySourceMetric
                {
                    Source = g.Key,
                    Count = g.Count()
                })
                .ToArray(),
            Runs = events
                .GroupBy(e => e.RunId)
                .OrderByDescending(g => g.Max(e => e.OccurredAt))
                .ThenBy(g => g.Key)
                .Select(g => new OperationalGlobalActivityRunMetric
                {
                    RunId = g.Key,
                    Count = g.Count(),
                    LastEventAt = g.Max(e => e.OccurredAt)
                })
                .ToArray()
        };
    }
}
