using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Validation;

/// <summary>
/// Provides known deployment validation script descriptors for Azure operationalization.
/// </summary>
public interface IAzureDeploymentValidationScriptRegistry
{
    IReadOnlyCollection<AzureDeploymentValidationScriptDescriptor> GetAll();

    IReadOnlyCollection<AzureDeploymentValidationScriptDescriptor> GetRequiredForEnvironment(string environmentName);

    AzureDeploymentValidationScriptDescriptor? FindByKey(string key);
}
