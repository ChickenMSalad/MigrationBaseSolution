namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsPresetDashboardService
    : IOperationalGlobalFailureAnalyticsPresetDashboardService
{
    private readonly IOperationalGlobalFailureAnalyticsPresetService _presetService;

    public OperationalGlobalFailureAnalyticsPresetDashboardService(
        IOperationalGlobalFailureAnalyticsPresetService presetService)
    {
        _presetService = presetService;
    }

    public async Task<OperationalGlobalFailureAnalyticsPresetDashboardResponse?> GetDashboardAsync(
        string presetKey = "all-recent",
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);
        var safePresetKey = string.IsNullOrWhiteSpace(presetKey)
            ? "all-recent"
            : presetKey.Trim();

        var catalog = await _presetService.GetPresetCatalogAsync(
            safeLimit,
            cancellationToken);

        var selectedPreset = await _presetService.GetPresetAnalyticsAsync(
            safePresetKey,
            safeLimit,
            cancellationToken);

        if (selectedPreset is null)
        {
            return null;
        }

        return new OperationalGlobalFailureAnalyticsPresetDashboardResponse
        {
            Catalog = catalog,
            SelectedPreset = selectedPreset,
            GeneratedAt = DateTimeOffset.UtcNow,
            Messages = BuildMessages(catalog, selectedPreset)
        };
    }

    private static IReadOnlyCollection<string> BuildMessages(
        OperationalGlobalFailureAnalyticsPresetCatalogResponse catalog,
        OperationalGlobalFailureAnalyticsPresetResponse selectedPreset)
    {
        return new[]
        {
            $"Preset catalog contains {catalog.Count} preset(s).",
            $"Selected preset is {selectedPreset.Preset.PresetKey}.",
            $"Selected preset returned {selectedPreset.Analytics.Results.Count} failure(s).",
            $"Selected preset metrics include {selectedPreset.Analytics.Metrics.TotalFailureCount} failure(s)."
        };
    }
}


