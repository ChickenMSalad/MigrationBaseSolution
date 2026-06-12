namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsPresetService
    : IOperationalGlobalFailureAnalyticsPresetService
{
    private readonly IOperationalGlobalFailureFilteredAnalyticsService _filteredAnalyticsService;

    public OperationalGlobalFailureAnalyticsPresetService(
        IOperationalGlobalFailureFilteredAnalyticsService filteredAnalyticsService)
    {
        _filteredAnalyticsService = filteredAnalyticsService;
    }

    public Task<OperationalGlobalFailureAnalyticsPresetCatalogResponse> GetPresetCatalogAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);
        var presets = BuildPresets(safeLimit);

        return Task.FromResult(new OperationalGlobalFailureAnalyticsPresetCatalogResponse
        {
            Count = presets.Length,
            GeneratedAt = DateTimeOffset.UtcNow,
            Presets = presets
        });
    }

    public async Task<OperationalGlobalFailureAnalyticsPresetResponse?> GetPresetAnalyticsAsync(
        string presetKey,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(presetKey))
        {
            return null;
        }

        var safeLimit = Math.Clamp(limit, 1, 500);

        var preset = BuildPresets(safeLimit)
            .FirstOrDefault(p =>
                p.PresetKey.Equals(
                    presetKey.Trim(),
                    StringComparison.OrdinalIgnoreCase));

        if (preset is null)
        {
            return null;
        }

        var analytics = await _filteredAnalyticsService.GetAnalyticsAsync(
            preset.Query,
            cancellationToken);

        return new OperationalGlobalFailureAnalyticsPresetResponse
        {
            Preset = preset,
            Analytics = analytics,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    private static OperationalGlobalFailureAnalyticsPreset[] BuildPresets(int limit)
    {
        return new[]
        {
            new OperationalGlobalFailureAnalyticsPreset
            {
                PresetKey = "all-recent",
                DisplayName = "All recent failures",
                Description = "Recent operational failures without additional filters.",
                Query = new OperationalGlobalFailureQuery { Limit = limit }
            },
            new OperationalGlobalFailureAnalyticsPreset
            {
                PresetKey = "retriable",
                DisplayName = "Retriable failures",
                Description = "Failures marked as retriable.",
                Query = new OperationalGlobalFailureQuery { IsRetriable = true, Limit = limit }
            },
            new OperationalGlobalFailureAnalyticsPreset
            {
                PresetKey = "non-retriable",
                DisplayName = "Non-retriable failures",
                Description = "Failures marked as non-retriable.",
                Query = new OperationalGlobalFailureQuery { IsRetriable = false, Limit = limit }
            },
            new OperationalGlobalFailureAnalyticsPreset
            {
                PresetKey = "failed-runs",
                DisplayName = "Failures on failed runs",
                Description = "Failures associated with runs currently marked Failed.",
                Query = new OperationalGlobalFailureQuery { SearchText = "Failed", Limit = limit }
            },
            new OperationalGlobalFailureAnalyticsPreset
            {
                PresetKey = "work-item-failures",
                DisplayName = "Work item failures",
                Description = "Failures with work item context.",
                Query = new OperationalGlobalFailureQuery { SearchText = "WorkItem", Limit = limit }
            }
        };
    }
}


