namespace MigrationBase.Core.Cloud.Azure.Tenancy;

public interface IAzureOperationalTenantBoundaryRegistry
{
    IReadOnlyCollection<AzureOperationalTenantBoundary> GetAll();

    AzureOperationalTenantBoundary? FindByName(string name);

    AzureOperationalTenantBoundaryValidationResult Validate(AzureOperationalTenantBoundary boundary);
}
