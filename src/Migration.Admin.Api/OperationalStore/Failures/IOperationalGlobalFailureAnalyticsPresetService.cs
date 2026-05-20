namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureAnalyticsPresetService
{
    Task<OperationalGlobalFailureAnalyticsPresetCatalogResponse> GetPresetCatalogAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<OperationalGlobalFailureAnalyticsPresetResponse?> GetPresetAnalyticsAsync(
        string presetKey,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
