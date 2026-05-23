namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Provides deployment parameter descriptors used by deployment automation, validation gates, and operator documentation.
/// </summary>
public interface IAzureDeploymentParameterRegistry
{
    IReadOnlyCollection<AzureDeploymentParameterDescriptor> GetAll();

    AzureDeploymentParameterDescriptor? FindByName(string name);

    AzureDeploymentParameterValidationResult ValidateRequiredParameters(
        IReadOnlyDictionary<string, string?> suppliedParameters,
        string environmentName,
        string hostRole);
}
