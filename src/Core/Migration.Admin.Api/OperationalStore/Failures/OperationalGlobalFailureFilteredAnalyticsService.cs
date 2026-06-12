namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureFilteredAnalyticsService
    : IOperationalGlobalFailureFilteredAnalyticsService
{
    private readonly IOperationalGlobalFailureQueryService _queryService;

    public OperationalGlobalFailureFilteredAnalyticsService(
        IOperationalGlobalFailureQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<OperationalGlobalFailureFilteredAnalyticsResponse> GetAnalyticsAsync(
        OperationalGlobalFailureQuery query,
        CancellationToken cancellationToken = default)
    {
        query ??= new OperationalGlobalFailureQuery();

        var safeQuery = new OperationalGlobalFailureQuery
        {
            RunId = query.RunId,
            FailureType = query.FailureType,
            IsRetriable = query.IsRetriable,
            SourceSystem = query.SourceSystem,
            TargetSystem = query.TargetSystem,
            SearchText = query.SearchText,
            Limit = Math.Clamp(query.Limit, 1, 500)
        };

        var results = await _queryService.QueryRecentFailuresAsync(
            safeQuery,
            cancellationToken);

        var failures = results.Failures.ToArray();

        var metrics = new OperationalGlobalFailureMetricsResponse
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

        return new OperationalGlobalFailureFilteredAnalyticsResponse
        {
            Query = safeQuery,
            Results = results,
            Metrics = metrics,
            GeneratedAt = DateTimeOffset.UtcNow,
            Messages = BuildMessages(safeQuery, results, metrics)
        };
    }

    private static IReadOnlyCollection<string> BuildMessages(
        OperationalGlobalFailureQuery query,
        OperationalGlobalRecentFailuresResponse results,
        OperationalGlobalFailureMetricsResponse metrics)
    {
        var messages = new List<string>
        {
            $"Filtered failure query returned {results.Count} failure(s).",
            $"Filtered failure metrics include {metrics.TotalFailureCount} failure(s)."
        };

        if (query.RunId is not null && query.RunId.Value != Guid.Empty)
        {
            messages.Add($"Filter applied: RunId={query.RunId}.");
        }

        if (!string.IsNullOrWhiteSpace(query.FailureType))
        {
            messages.Add($"Filter applied: FailureType={query.FailureType}.");
        }

        if (query.IsRetriable is not null)
        {
            messages.Add($"Filter applied: IsRetriable={query.IsRetriable.Value}.");
        }

        if (!string.IsNullOrWhiteSpace(query.SourceSystem))
        {
            messages.Add($"Filter applied: SourceSystem={query.SourceSystem}.");
        }

        if (!string.IsNullOrWhiteSpace(query.TargetSystem))
        {
            messages.Add($"Filter applied: TargetSystem={query.TargetSystem}.");
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            messages.Add($"Filter applied: SearchText={query.SearchText}.");
        }

        return messages;
    }
}


