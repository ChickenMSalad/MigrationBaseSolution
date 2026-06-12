namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineSearchService
    : IOperationalRunTimelineSearchService
{
    private readonly IOperationalRunTimelineService _timelineService;

    public OperationalRunTimelineSearchService(
        IOperationalRunTimelineService timelineService)
    {
        _timelineService = timelineService;
    }

    public async Task<OperationalRunTimelineResponse?> SearchAsync(
        Guid runId,
        OperationalRunTimelineSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        query ??= new OperationalRunTimelineSearchQuery();

        var fullTimeline = await _timelineService.GetTimelineAsync(
            runId,
            cancellationToken);

        if (fullTimeline is null)
        {
            return null;
        }

        var searchText = query.SearchText?.Trim();

        IEnumerable<OperationalRunTimelineEvent> matches = fullTimeline.Events;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            matches = matches.Where(e =>
                Contains(e.EventType, searchText) ||
                Contains(e.Source, searchText) ||
                Contains(e.Message, searchText) ||
                Contains(e.WorkItemId?.ToString(), searchText) ||
                Contains(e.ManifestRecordId?.ToString(), searchText) ||
                Contains(e.CheckpointId?.ToString(), searchText) ||
                Contains(e.FailureId?.ToString(), searchText));
        }

        var limited = matches
            .Take(Math.Clamp(query.Limit, 1, 1000))
            .ToArray();

        return new OperationalRunTimelineResponse
        {
            RunId = fullTimeline.RunId,
            EventCount = limited.Length,
            Events = limited
        };
    }

    private static bool Contains(
        string? value,
        string searchText)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}


