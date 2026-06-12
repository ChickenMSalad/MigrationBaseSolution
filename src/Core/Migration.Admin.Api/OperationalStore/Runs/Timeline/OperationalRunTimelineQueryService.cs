namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineQueryService
    : IOperationalRunTimelineQueryService
{
    private readonly IOperationalRunTimelineService _timelineService;

    public OperationalRunTimelineQueryService(
        IOperationalRunTimelineService timelineService)
    {
        _timelineService = timelineService;
    }

    public async Task<OperationalRunTimelineResponse?> QueryTimelineAsync(
        Guid runId,
        OperationalRunTimelineQuery query,
        CancellationToken cancellationToken = default)
    {
        query ??= new OperationalRunTimelineQuery();

        var fullTimeline = await _timelineService.GetTimelineAsync(
            runId,
            cancellationToken);

        if (fullTimeline is null)
        {
            return null;
        }

        IEnumerable<OperationalRunTimelineEvent> filtered = fullTimeline.Events;

        if (!string.IsNullOrWhiteSpace(query.EventType))
        {
            filtered = filtered.Where(e =>
                e.EventType.Equals(
                    query.EventType,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            filtered = filtered.Where(e =>
                e.Source.Equals(
                    query.Source,
                    StringComparison.OrdinalIgnoreCase));
        }

        var limited = filtered
            .Take(Math.Clamp(query.Limit, 1, 1000))
            .ToArray();

        return new OperationalRunTimelineResponse
        {
            RunId = fullTimeline.RunId,
            EventCount = limited.Length,
            Events = limited
        };
    }
}


