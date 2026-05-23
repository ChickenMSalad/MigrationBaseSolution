namespace MigrationBase.Core.Cloud.Azure.Infrastructure.Identity;

public interface IAzureCredentialBoundaryRegistry
{
    IReadOnlyCollection<AzureCredentialBoundary> GetAll();

    AzureCredentialBoundary? FindByName(string name);

    void Add(AzureCredentialBoundary boundary);
}
