namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureSystemPairMetricsService
    : IOperationalGlobalFailureSystemPairMetricsService
{
    private readonly IOperationalGlobalFailureService _failureService;

    public OperationalGlobalFailureSystemPairMetricsService(
        IOperationalGlobalFailureService failureService)
    {
        _failureService = failureService;
    }

    public async Task<OperationalGlobalFailureSystemPairMetricsResponse> GetMetricsAsync(
        int sampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(sampleLimit, 1, 500);

        var recentFailures = await _failureService.GetRecentFailuresAsync(
            safeLimit,
            cancellationToken);

        var failures = recentFailures.Failures.ToArray();

        var systemPairs = failures
            .GroupBy(f => new
            {
                SourceSystem = f.SourceSystem ?? string.Empty,
                TargetSystem = f.TargetSystem ?? string.Empty
            })
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key.SourceSystem, StringComparer.OrdinalIgnoreCase)
            .ThenBy(g => g.Key.TargetSystem, StringComparer.OrdinalIgnoreCase)
            .Select(g => new OperationalGlobalFailureSystemPairDetailMetric
            {
                SourceSystem = g.Key.SourceSystem,
                TargetSystem = g.Key.TargetSystem,
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

        return new OperationalGlobalFailureSystemPairMetricsResponse
        {
            TotalFailureCount = failures.Length,
            SystemPairCount = systemPairs.Length,
            GeneratedAt = DateTimeOffset.UtcNow,
            SystemPairs = systemPairs
        };
    }
}

