namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureCatalogService
{
    Task<OperationalGlobalFailureCatalogResponse> GetCatalogAsync(
        int sampleLimit = 500,
        CancellationToken cancellationToken = default);
}


