namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsPresetFavoritesResponse
{
    public int Count { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<OperationalGlobalFailureAnalyticsPresetFavorite> Favorites { get; init; } =
        Array.Empty<OperationalGlobalFailureAnalyticsPresetFavorite>();
}
