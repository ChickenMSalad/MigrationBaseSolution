namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionPlanStore
{
    Task<IReadOnlyList<ExecutionPlanStepRecord>> SeedDefaultPlanAsync(
        SeedExecutionPlanRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecutionPlanStepRecord>> ReadPlanAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken);
}
