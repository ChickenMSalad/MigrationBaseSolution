namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureService
{
    Task<OperationalGlobalRecentFailuresResponse> GetRecentFailuresAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);
}
