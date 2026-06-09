namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalRunHealthOperationsCenterService
{
    Task<OperationalGlobalRunHealthOperationsCenterResponse> GetOperationsCenterAsync(
        int activityRecentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default);
}


