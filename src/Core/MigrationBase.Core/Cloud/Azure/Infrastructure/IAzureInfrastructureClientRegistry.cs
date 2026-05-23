namespace MigrationBase.Core.Cloud.Azure.Infrastructure;

public interface IAzureInfrastructureClientRegistry
{
    IReadOnlyCollection<AzureInfrastructureClientDescriptor> GetClients();

    AzureInfrastructureClientDescriptor? FindByName(string name);

    AzureInfrastructureClientValidationResult Validate();
}
