namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunStatusProjectionService
{
    Task<IReadOnlyCollection<OperationalRunStatusProjection>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalRunStatusProjection?> GetAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
}


