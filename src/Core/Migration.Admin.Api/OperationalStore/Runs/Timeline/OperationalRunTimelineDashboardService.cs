namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineDashboardService
    : IOperationalRunTimelineDashboardService
{
    private readonly IOperationalRunDashboardSummaryService _runDashboardService;
    private readonly IOperationalRunTimelineMetricsService _timelineMetricsService;
    private readonly IOperationalRunTimelineQueryService _timelineQueryService;

    public OperationalRunTimelineDashboardService(
        IOperationalRunDashboardSummaryService runDashboardService,
        IOperationalRunTimelineMetricsService timelineMetricsService,
        IOperationalRunTimelineQueryService timelineQueryService)
    {
        _runDashboardService = runDashboardService;
        _timelineMetricsService = timelineMetricsService;
        _timelineQueryService = timelineQueryService;
    }

    public async Task<OperationalRunTimelineDashboardResponse?> GetDashboardAsync(
        Guid runId,
        int previewLimit = 10,
        CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            throw new ArgumentException("RunId is required.", nameof(runId));
        }

        var runDashboard = await _runDashboardService.GetSummaryAsync(
            runId,
            cancellationToken);

        if (runDashboard is null)
        {
            return null;
        }

        var timelineMetrics = await _timelineMetricsService.GetMetricsAsync(
            runId,
            cancellationToken);

        if (timelineMetrics is null)
        {
            return null;
        }

        var timelinePreview = await _timelineQueryService.QueryTimelineAsync(
            runId,
            new OperationalRunTimelineQuery
            {
                Limit = Math.Clamp(previewLimit, 1, 100)
            },
            cancellationToken);

        if (timelinePreview is null)
        {
            return null;
        }

        return new OperationalRunTimelineDashboardResponse
        {
            RunId = runId,
            RunDashboard = runDashboard,
            TimelineMetrics = timelineMetrics,
            TimelinePreview = timelinePreview,
            GeneratedAt = DateTimeOffset.UtcNow,
            Messages = BuildMessages(runDashboard, timelineMetrics, timelinePreview)
        };
    }

    private static IReadOnlyCollection<string> BuildMessages(
        OperationalRunDashboardSummaryResponse runDashboard,
        OperationalRunTimelineMetricsResponse timelineMetrics,
        OperationalRunTimelineResponse timelinePreview)
    {
        var messages = new List<string>();

        messages.AddRange(runDashboard.Messages);

        messages.Add(
            $"Timeline contains {timelineMetrics.TotalEventCount} event(s).");

        if (timelineMetrics.FirstEventAt is not null && timelineMetrics.LastEventAt is not null)
        {
            messages.Add(
                $"Timeline spans {timelineMetrics.FirstEventAt} through {timelineMetrics.LastEventAt}.");
        }

        messages.Add(
            $"Timeline preview returned {timelinePreview.EventCount} event(s).");

        return messages;
    }
}


