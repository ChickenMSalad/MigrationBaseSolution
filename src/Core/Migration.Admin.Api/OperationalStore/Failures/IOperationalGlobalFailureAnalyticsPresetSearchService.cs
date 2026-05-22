namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureAnalyticsPresetSearchService
{
    Task<OperationalGlobalFailureAnalyticsPresetSearchResponse> SearchAsync(
        string? searchText,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
