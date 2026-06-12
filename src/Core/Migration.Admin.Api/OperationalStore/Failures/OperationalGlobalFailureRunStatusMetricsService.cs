namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureRunStatusMetricsService
    : IOperationalGlobalFailureRunStatusMetricsService
{
    private readonly IOperationalGlobalFailureService _failureService;

    public OperationalGlobalFailureRunStatusMetricsService(
        IOperationalGlobalFailureService failureService)
    {
        _failureService = failureService;
    }

    public async Task<OperationalGlobalFailureRunStatusMetricsResponse> GetMetricsAsync(
        int sampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(sampleLimit, 1, 500);

        var recentFailures = await _failureService.GetRecentFailuresAsync(
            safeLimit,
            cancellationToken);

        var failures = recentFailures.Failures.ToArray();

        var runStatuses = failures
            .GroupBy(f => f.RunStatus ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new OperationalGlobalFailureRunStatusDetailMetric
            {
                RunStatus = g.Key,
                Count = g.Count(),
                RetriableCount = g.Count(f => f.IsRetriable),
                NonRetriableCount = g.Count(f => !f.IsRetriable),
                FirstFailureAt = g.Min(f => f.CreatedAt),
                LastFailureAt = g.Max(f => f.CreatedAt),
                FailureTypes = g
                    .GroupBy(f => f.FailureType, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(fg => fg.Count())
                    .ThenBy(fg => fg.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(fg => new OperationalGlobalFailureTypeMetric
                    {
                        FailureType = fg.Key,
                        Count = fg.Count(),
                        RetriableCount = fg.Count(f => f.IsRetriable),
                        NonRetriableCount = fg.Count(f => !f.IsRetriable)
                    })
                    .ToArray()
            })
            .ToArray();

        return new OperationalGlobalFailureRunStatusMetricsResponse
        {
            TotalFailureCount = failures.Length,
            RunStatusCount = runStatuses.Length,
            GeneratedAt = DateTimeOffset.UtcNow,
            RunStatuses = runStatuses
        };
    }
}


