namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalDispatcherDiagnosticsService
{
    Task<OperationalDispatcherDiagnosticsResponse> GetDiagnosticsAsync(
        CancellationToken cancellationToken = default);
}
