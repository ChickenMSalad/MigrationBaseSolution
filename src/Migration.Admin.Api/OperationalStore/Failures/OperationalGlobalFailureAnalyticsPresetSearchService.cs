namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsPresetSearchService
    : IOperationalGlobalFailureAnalyticsPresetSearchService
{
    private readonly IOperationalGlobalFailureAnalyticsPresetService _presetService;

    public OperationalGlobalFailureAnalyticsPresetSearchService(
        IOperationalGlobalFailureAnalyticsPresetService presetService)
    {
        _presetService = presetService;
    }

    public async Task<OperationalGlobalFailureAnalyticsPresetSearchResponse> SearchAsync(
        string? searchText,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);
        var normalizedSearchText = searchText?.Trim() ?? string.Empty;

        var catalog = await _presetService.GetPresetCatalogAsync(
            safeLimit,
            cancellationToken);

        IEnumerable<OperationalGlobalFailureAnalyticsPreset> presets = catalog.Presets;

        if (!string.IsNullOrWhiteSpace(normalizedSearchText))
        {
            presets = presets.Where(p =>
                Contains(p.PresetKey, normalizedSearchText) ||
                Contains(p.DisplayName, normalizedSearchText) ||
                Contains(p.Description, normalizedSearchText) ||
                Contains(p.Query.FailureType, normalizedSearchText) ||
                Contains(p.Query.SourceSystem, normalizedSearchText) ||
                Contains(p.Query.TargetSystem, normalizedSearchText) ||
                Contains(p.Query.SearchText, normalizedSearchText) ||
                Contains(p.Query.IsRetriable?.ToString(), normalizedSearchText));
        }

        var matched = presets
            .Take(safeLimit)
            .ToArray();

        return new OperationalGlobalFailureAnalyticsPresetSearchResponse
        {
            SearchText = normalizedSearchText,
            Count = matched.Length,
            GeneratedAt = DateTimeOffset.UtcNow,
            Presets = matched
        };
    }

    private static bool Contains(string? value, string searchText)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}
