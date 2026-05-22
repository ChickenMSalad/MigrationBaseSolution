namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalRunHealthDetailedRiskService
{
    Task<OperationalGlobalRunHealthDetailedRiskResponse> GetDetailedRiskAsync(
        int recentLimit = 25,
        int metricsSampleLimit = 500,
        CancellationToken cancellationToken = default);
}
