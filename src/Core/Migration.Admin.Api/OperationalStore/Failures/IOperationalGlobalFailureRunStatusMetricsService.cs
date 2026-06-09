namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureRunStatusMetricsService
{
    Task<OperationalGlobalFailureRunStatusMetricsResponse> GetMetricsAsync(
        int sampleLimit = 500,
        CancellationToken cancellationToken = default);
}


