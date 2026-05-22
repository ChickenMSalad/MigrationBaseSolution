namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureSystemPairMetricsService
{
    Task<OperationalGlobalFailureSystemPairMetricsResponse> GetMetricsAsync(
        int sampleLimit = 500,
        CancellationToken cancellationToken = default);
}
