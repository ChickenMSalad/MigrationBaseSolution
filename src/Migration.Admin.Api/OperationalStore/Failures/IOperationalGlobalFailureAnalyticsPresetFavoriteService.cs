namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureAnalyticsPresetFavoriteService
{
    Task<OperationalGlobalFailureAnalyticsPresetFavoritesResponse> GetFavoritesAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalGlobalFailureAnalyticsPresetFavoriteDashboardResponse?> GetFavoriteDashboardAsync(
        string favoriteKey,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
