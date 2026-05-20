namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureMetricsService
{
    Task<OperationalGlobalFailureMetricsResponse> GetMetricsAsync(
        int sampleLimit = 500,
        CancellationToken cancellationToken = default);
}
