namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalActivityQueryService
    : IOperationalGlobalActivityQueryService
{
    private readonly IOperationalGlobalActivityFeedService _activityFeedService;

    public OperationalGlobalActivityQueryService(
        IOperationalGlobalActivityFeedService activityFeedService)
    {
        _activityFeedService = activityFeedService;
    }

    public async Task<OperationalGlobalActivityFeedResponse> QueryRecentActivityAsync(
        OperationalGlobalActivityQuery query,
        CancellationToken cancellationToken = default)
    {
        query ??= new OperationalGlobalActivityQuery();

        var requestedLimit = Math.Clamp(query.Limit, 1, 500);

        // Pull a wider recent window before in-memory filtering so filtered dashboards
        // are useful without introducing another large SQL union variant.
        var sourceLimit = Math.Clamp(requestedLimit * 10, requestedLimit, 500);

        var feed = await _activityFeedService.GetRecentActivityAsync(
            sourceLimit,
            cancellationToken);

        IEnumerable<OperationalGlobalActivityEvent> filtered = feed.Events;

        if (query.RunId is not null && query.RunId.Value != Guid.Empty)
        {
            filtered = filtered.Where(e => e.RunId == query.RunId.Value);
        }

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

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searchText = query.SearchText.Trim();

            filtered = filtered.Where(e =>
                Contains(e.EventType, searchText) ||
                Contains(e.Source, searchText) ||
                Contains(e.Message, searchText) ||
                Contains(e.RunId.ToString(), searchText) ||
                Contains(e.WorkItemId?.ToString(), searchText) ||
                Contains(e.ManifestRecordId?.ToString(), searchText) ||
                Contains(e.CheckpointId?.ToString(), searchText) ||
                Contains(e.FailureId?.ToString(), searchText));
        }

        var events = filtered
            .Take(requestedLimit)
            .ToArray();

        return new OperationalGlobalActivityFeedResponse
        {
            EventCount = events.Length,
            Limit = requestedLimit,
            GeneratedAt = DateTimeOffset.UtcNow,
            Events = events
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
