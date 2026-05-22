namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunTimelineGlobalCatalogService
{
    Task<OperationalRunTimelineGlobalCatalogResponse> GetCatalogAsync(
        CancellationToken cancellationToken = default);
}
