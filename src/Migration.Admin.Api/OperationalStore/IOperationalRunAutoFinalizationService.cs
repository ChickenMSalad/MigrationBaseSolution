namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunAutoFinalizationService
{
    Task<int> FinalizeEligibleRunsAsync(
        CancellationToken cancellationToken = default);
}
