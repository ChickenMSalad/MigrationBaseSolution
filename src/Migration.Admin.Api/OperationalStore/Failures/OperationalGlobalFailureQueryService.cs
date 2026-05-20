namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureQueryService
    : IOperationalGlobalFailureQueryService
{
    private readonly IOperationalGlobalFailureService _failureService;

    public OperationalGlobalFailureQueryService(
        IOperationalGlobalFailureService failureService)
    {
        _failureService = failureService;
    }

    public async Task<OperationalGlobalRecentFailuresResponse> QueryRecentFailuresAsync(
        OperationalGlobalFailureQuery query,
        CancellationToken cancellationToken = default)
    {
        query ??= new OperationalGlobalFailureQuery();

        var requestedLimit = Math.Clamp(query.Limit, 1, 500);
        var sourceLimit = Math.Clamp(requestedLimit * 10, requestedLimit, 500);

        var recent = await _failureService.GetRecentFailuresAsync(
            sourceLimit,
            cancellationToken);

        IEnumerable<OperationalGlobalFailureItem> filtered = recent.Failures;

        if (query.RunId is not null && query.RunId.Value != Guid.Empty)
        {
            filtered = filtered.Where(f => f.RunId == query.RunId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.FailureType))
        {
            filtered = filtered.Where(f =>
                f.FailureType.Equals(
                    query.FailureType,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (query.IsRetriable is not null)
        {
            filtered = filtered.Where(f => f.IsRetriable == query.IsRetriable.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SourceSystem))
        {
            filtered = filtered.Where(f =>
                f.SourceSystem.Equals(
                    query.SourceSystem,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.TargetSystem))
        {
            filtered = filtered.Where(f =>
                f.TargetSystem.Equals(
                    query.TargetSystem,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searchText = query.SearchText.Trim();

            filtered = filtered.Where(f =>
                Contains(f.FailureId.ToString(), searchText) ||
                Contains(f.RunId.ToString(), searchText) ||
                Contains(f.ManifestRecordId?.ToString(), searchText) ||
                Contains(f.WorkItemId?.ToString(), searchText) ||
                Contains(f.FailureType, searchText) ||
                Contains(f.Message, searchText) ||
                Contains(f.Details, searchText) ||
                Contains(f.RunStatus, searchText) ||
                Contains(f.SourceSystem, searchText) ||
                Contains(f.TargetSystem, searchText) ||
                Contains(f.WorkItemStatus, searchText));
        }

        var failures = filtered
            .Take(requestedLimit)
            .ToArray();

        return new OperationalGlobalRecentFailuresResponse
        {
            Count = failures.Length,
            Limit = requestedLimit,
            GeneratedAt = DateTimeOffset.UtcNow,
            Failures = failures
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
