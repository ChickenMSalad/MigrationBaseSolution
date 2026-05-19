namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalDispatcherService
{
    Task<OperationalDispatcherRunOnceResponse> RunOnceAsync(
        CancellationToken cancellationToken = default);
}
