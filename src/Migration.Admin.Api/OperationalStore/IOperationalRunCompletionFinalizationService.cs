namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunCompletionFinalizationService
{
    Task<OperationalRunCompletionReadinessResponse> GetReadinessAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<OperationalRunCompletionReadinessResponse> FinalizeAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
}
