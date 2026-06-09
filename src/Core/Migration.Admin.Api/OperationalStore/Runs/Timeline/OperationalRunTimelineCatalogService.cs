namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineCatalogService
    : IOperationalRunTimelineCatalogService
{
    private readonly IOperationalRunTimelineMetricsService _metricsService;

    public OperationalRunTimelineCatalogService(
        IOperationalRunTimelineMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public async Task<OperationalRunTimelineCatalogResponse?> GetCatalogAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var metrics = await _metricsService.GetMetricsAsync(
            runId,
            cancellationToken);

        if (metrics is null)
        {
            return null;
        }

        var eventTypes = metrics.EventTypes
            .Select(e => e.EventType)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(e => e, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var sources = metrics.Sources
            .Select(s => s.Source)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new OperationalRunTimelineCatalogResponse
        {
            RunId = metrics.RunId,
            EventTypes = eventTypes,
            Sources = sources,
            EventTypeCount = eventTypes.Length,
            SourceCount = sources.Length
        };
    }
}


