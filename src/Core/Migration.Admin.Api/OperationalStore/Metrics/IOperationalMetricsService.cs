namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalMetricsService
{
    Task<OperationalWorkItemMetricsResponse> GetWorkItemMetricsAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalLeaseMetricsResponse> GetLeaseMetricsAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalRunMetricsResponse> GetRunMetricsAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalDiagnosticsSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default);
}


