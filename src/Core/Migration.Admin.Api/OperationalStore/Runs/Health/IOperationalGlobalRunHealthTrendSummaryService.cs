namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalRunHealthTrendSummaryService
{
    Task<OperationalGlobalRunHealthTrendSummaryResponse> GetTrendSummaryAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default);
}


