namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunStatusReconciliationService
{
    Task<OperationalRunStatusReconciliationResponse> PreviewAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<OperationalRunStatusReconciliationResponse> ApplyAsync(Guid runId, CancellationToken cancellationToken = default);
}


