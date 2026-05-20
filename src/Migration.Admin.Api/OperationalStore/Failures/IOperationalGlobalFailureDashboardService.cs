namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureDashboardService
{
    Task<OperationalGlobalFailureDashboardResponse> GetDashboardAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default);
}
