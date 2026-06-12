namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsPresetFavoriteService
    : IOperationalGlobalFailureAnalyticsPresetFavoriteService
{
    private readonly IOperationalGlobalFailureAnalyticsPresetService _presetService;

    public OperationalGlobalFailureAnalyticsPresetFavoriteService(
        IOperationalGlobalFailureAnalyticsPresetService presetService)
    {
        _presetService = presetService;
    }

    public Task<OperationalGlobalFailureAnalyticsPresetFavoritesResponse> GetFavoritesAsync(
        CancellationToken cancellationToken = default)
    {
        var favorites = BuildFavorites();

        return Task.FromResult(new OperationalGlobalFailureAnalyticsPresetFavoritesResponse
        {
            Count = favorites.Length,
            GeneratedAt = DateTimeOffset.UtcNow,
            Favorites = favorites
        });
    }

    public async Task<OperationalGlobalFailureAnalyticsPresetFavoriteDashboardResponse?> GetFavoriteDashboardAsync(
        string favoriteKey,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(favoriteKey))
        {
            return null;
        }

        var safeLimit = Math.Clamp(limit, 1, 500);
        var favorite = BuildFavorites()
            .FirstOrDefault(f =>
                f.FavoriteKey.Equals(
                    favoriteKey.Trim(),
                    StringComparison.OrdinalIgnoreCase));

        if (favorite is null)
        {
            return null;
        }

        var presetResponses = new List<OperationalGlobalFailureAnalyticsPresetResponse>();

        foreach (var presetKey in favorite.PresetKeys)
        {
            var presetResponse = await _presetService.GetPresetAnalyticsAsync(
                presetKey,
                safeLimit,
                cancellationToken);

            if (presetResponse is not null)
            {
                presetResponses.Add(presetResponse);
            }
        }

        return new OperationalGlobalFailureAnalyticsPresetFavoriteDashboardResponse
        {
            Favorite = favorite,
            Presets = presetResponses,
            Count = presetResponses.Count,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    private static OperationalGlobalFailureAnalyticsPresetFavorite[] BuildFavorites()
    {
        return new[]
        {
            new OperationalGlobalFailureAnalyticsPresetFavorite
            {
                FavoriteKey = "triage",
                DisplayName = "Triage",
                Description = "Most useful presets for first-pass operational failure triage.",
                PresetKeys = new[]
                {
                    "all-recent",
                    "retriable",
                    "non-retriable"
                }
            },
            new OperationalGlobalFailureAnalyticsPresetFavorite
            {
                FavoriteKey = "recovery",
                DisplayName = "Recovery",
                Description = "Presets focused on recoverable work and rerun planning.",
                PresetKeys = new[]
                {
                    "retriable",
                    "work-item-failures"
                }
            },
            new OperationalGlobalFailureAnalyticsPresetFavorite
            {
                FavoriteKey = "run-health",
                DisplayName = "Run health",
                Description = "Presets focused on failed runs and broad failure health.",
                PresetKeys = new[]
                {
                    "failed-runs",
                    "all-recent"
                }
            }
        };
    }
}


