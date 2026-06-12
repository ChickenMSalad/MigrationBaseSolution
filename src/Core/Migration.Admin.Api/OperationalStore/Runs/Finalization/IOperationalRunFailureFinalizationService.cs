namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunFailureFinalizationService
{
    Task<OperationalRunFailureReadinessResponse> GetReadinessAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<OperationalRunFailureReadinessResponse> FinalizeAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
}


