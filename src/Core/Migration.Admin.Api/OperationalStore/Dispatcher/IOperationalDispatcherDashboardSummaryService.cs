namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalDispatcherDashboardSummaryService
{
    Task<OperationalDispatcherDashboardSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default);
}


