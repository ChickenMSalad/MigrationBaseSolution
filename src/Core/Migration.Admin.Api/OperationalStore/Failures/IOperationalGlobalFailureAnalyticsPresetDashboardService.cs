namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureAnalyticsPresetDashboardService
{
    Task<OperationalGlobalFailureAnalyticsPresetDashboardResponse?> GetDashboardAsync(
        string presetKey = "all-recent",
        int limit = 50,
        CancellationToken cancellationToken = default);
}
