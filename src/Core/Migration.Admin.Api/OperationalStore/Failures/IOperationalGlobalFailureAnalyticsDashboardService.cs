namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureAnalyticsDashboardService
{
    Task<OperationalGlobalFailureAnalyticsDashboardResponse> GetDashboardAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default);
}


