namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunControlService
{
    Task<OperationalRunControlStateResponse> GetControlStateAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<OperationalRunControlStateResponse> RequestCancelAsync(
        Guid runId,
        OperationalRunControlActionRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalRunControlStateResponse> AbortAsync(
        Guid runId,
        OperationalRunControlActionRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalRunControlStateResponse> ResumeAsync(
        Guid runId,
        OperationalRunControlActionRequest request,
        CancellationToken cancellationToken = default);
}


