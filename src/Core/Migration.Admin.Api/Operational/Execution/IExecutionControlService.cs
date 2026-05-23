namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionControlService
{
    Task PauseAsync(
        PauseExecutionSessionRequest request,
        CancellationToken cancellationToken);

    Task ResumeAsync(
        ResumeExecutionSessionRequest request,
        CancellationToken cancellationToken);
}
