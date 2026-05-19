namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineMetricsService
    : IOperationalRunTimelineMetricsService
{
    private readonly IOperationalRunTimelineService _timelineService;

    public OperationalRunTimelineMetricsService(
        IOperationalRunTimelineService timelineService)
    {
        _timelineService = timelineService;
    }

    public async Task<OperationalRunTimelineMetricsResponse?> GetMetricsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var timeline = await _timelineService.GetTimelineAsync(
            runId,
            cancellationToken);

        if (timeline is null)
        {
            return null;
        }

        var events = timeline.Events.ToArray();

        return new OperationalRunTimelineMetricsResponse
        {
            RunId = timeline.RunId,
            TotalEventCount = events.Length,
            FirstEventAt = events.Length == 0 ? null : events.Min(e => e.OccurredAt),
            LastEventAt = events.Length == 0 ? null : events.Max(e => e.OccurredAt),
            EventTypes = events
                .GroupBy(e => e.EventType, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new OperationalRunTimelineEventTypeMetric
                {
                    EventType = g.Key,
                    Count = g.Count()
                })
                .ToArray(),
            Sources = events
                .GroupBy(e => e.Source, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new OperationalRunTimelineSourceMetric
                {
                    Source = g.Key,
                    Count = g.Count()
                })
                .ToArray()
        };
    }
}
