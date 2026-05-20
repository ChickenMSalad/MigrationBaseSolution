namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalActivityMetricsService
{
    Task<OperationalGlobalActivityMetricsResponse> GetMetricsAsync(
        int sampleLimit = 500,
        CancellationToken cancellationToken = default);
}
