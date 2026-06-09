namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalRunHealthDashboardService
{
    Task<OperationalGlobalRunHealthDashboardResponse> GetDashboardAsync(
        int activityLimit = 10,
        int failureLimit = 10,
        int metricsSampleLimit = 100,
        CancellationToken cancellationToken = default);
}


