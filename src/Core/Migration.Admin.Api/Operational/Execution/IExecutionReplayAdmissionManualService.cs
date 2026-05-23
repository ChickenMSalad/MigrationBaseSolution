namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionReplayAdmissionManualService
{
    Task<ReplayAdmissionManualDecisionResult> ForceAdmitAsync(
        ReplayAdmissionManualDecisionRequest request,
        CancellationToken cancellationToken);

    Task<ReplayAdmissionManualDecisionResult> ForceDeferAsync(
        ReplayAdmissionManualDecisionRequest request,
        CancellationToken cancellationToken);
}
