namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalRunHealthSummaryService
{
    Task<OperationalGlobalRunHealthSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default);
}
