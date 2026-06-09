namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalRunHealthRecommendationService
{
    Task<OperationalGlobalRunHealthRecommendationsResponse> GetRecommendationsAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default);
}


