namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunTimelineDashboardService
{
    Task<OperationalRunTimelineDashboardResponse?> GetDashboardAsync(
        Guid runId,
        int previewLimit = 10,
        CancellationToken cancellationToken = default);
}
