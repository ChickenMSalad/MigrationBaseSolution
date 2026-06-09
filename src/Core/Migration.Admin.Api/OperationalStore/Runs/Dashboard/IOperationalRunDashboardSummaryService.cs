namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunDashboardSummaryService
{
    Task<OperationalRunDashboardSummaryResponse?> GetSummaryAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
}


