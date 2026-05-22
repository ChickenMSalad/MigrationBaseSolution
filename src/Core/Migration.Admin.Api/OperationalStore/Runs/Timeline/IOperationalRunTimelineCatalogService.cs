namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunTimelineCatalogService
{
    Task<OperationalRunTimelineCatalogResponse?> GetCatalogAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
}
