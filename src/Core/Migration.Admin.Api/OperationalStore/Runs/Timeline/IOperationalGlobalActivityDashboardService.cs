namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalActivityDashboardService
{
    Task<OperationalGlobalActivityDashboardResponse> GetDashboardAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default);
}
