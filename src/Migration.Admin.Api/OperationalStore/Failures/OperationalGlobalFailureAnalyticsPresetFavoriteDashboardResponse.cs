namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsPresetFavoriteDashboardResponse
{
    public OperationalGlobalFailureAnalyticsPresetFavorite Favorite { get; init; } = default!;

    public IReadOnlyCollection<OperationalGlobalFailureAnalyticsPresetResponse> Presets { get; init; } =
        Array.Empty<OperationalGlobalFailureAnalyticsPresetResponse>();

    public int Count { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }
}
