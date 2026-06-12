namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalRunHealthActionPlanService
{
    Task<OperationalGlobalRunHealthActionPlanResponse> GetActionPlanAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default);
}


