namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureMetricsService
    : IOperationalGlobalFailureMetricsService
{
    private readonly IOperationalGlobalFailureService _failureService;

    public OperationalGlobalFailureMetricsService(
        IOperationalGlobalFailureService failureService)
    {
        _failureService = failureService;
    }

    public async Task<OperationalGlobalFailureMetricsResponse> GetMetricsAsync(
        int sampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(sampleLimit, 1, 500);

        var response = await _failureService.GetRecentFailuresAsync(
            safeLimit,
            cancellationToken);

        var failures = response.Failures.ToArray();

        return new OperationalGlobalFailureMetricsResponse
        {
            TotalFailureCount = failures.Length,
            RetriableFailureCount = failures.Count(f => f.IsRetriable),
            NonRetriableFailureCount = failures.Count(f => !f.IsRetriable),
            FirstFailureAt = failures.Length == 0 ? null : failures.Min(f => f.CreatedAt),
            LastFailureAt = failures.Length == 0 ? null : failures.Max(f => f.CreatedAt),
            GeneratedAt = DateTimeOffset.UtcNow,
            FailureTypes = failures
                .GroupBy(f => f.FailureType, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new OperationalGlobalFailureTypeMetric
                {
                    FailureType = g.Key,
                    Count = g.Count(),
                    RetriableCount = g.Count(f => f.IsRetriable),
                    NonRetriableCount = g.Count(f => !f.IsRetriable)
                })
                .ToArray(),
            RunStatuses = failures
                .GroupBy(f => f.RunStatus, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new OperationalGlobalFailureRunStatusMetric
                {
                    RunStatus = g.Key,
                    Count = g.Count()
                })
                .ToArray(),
            SystemPairs = failures
                .GroupBy(f => new
                {
                    SourceSystem = f.SourceSystem ?? string.Empty,
                    TargetSystem = f.TargetSystem ?? string.Empty
                })
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key.SourceSystem, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Key.TargetSystem, StringComparer.OrdinalIgnoreCase)
                .Select(g => new OperationalGlobalFailureSystemPairMetric
                {
                    SourceSystem = g.Key.SourceSystem,
                    TargetSystem = g.Key.TargetSystem,
                    Count = g.Count()
                })
                .ToArray()
        };
    }
}


