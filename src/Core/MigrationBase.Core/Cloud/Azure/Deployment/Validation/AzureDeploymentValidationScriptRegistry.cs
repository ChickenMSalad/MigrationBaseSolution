using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Validation;

/// <summary>
/// In-memory registry for deployment validation script descriptors.
/// Later P5.3 sets can bind this from deployment manifests or generated pipeline metadata.
/// </summary>
public sealed class AzureDeploymentValidationScriptRegistry : IAzureDeploymentValidationScriptRegistry
{
    private readonly IReadOnlyList<AzureDeploymentValidationScriptDescriptor> _scripts;

    public AzureDeploymentValidationScriptRegistry(IEnumerable<AzureDeploymentValidationScriptDescriptor> scripts)
    {
        if (scripts is null)
        {
            throw new ArgumentNullException(nameof(scripts));
        }

        _scripts = new ReadOnlyCollection<AzureDeploymentValidationScriptDescriptor>(
            scripts
                .GroupBy(script => script.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(script => script.Key, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    public IReadOnlyCollection<AzureDeploymentValidationScriptDescriptor> GetAll()
    {
        return new ReadOnlyCollection<AzureDeploymentValidationScriptDescriptor>(_scripts.ToList());
    }

    public IReadOnlyCollection<AzureDeploymentValidationScriptDescriptor> GetRequiredForEnvironment(string environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return Array.Empty<AzureDeploymentValidationScriptDescriptor>();
        }

        return new ReadOnlyCollection<AzureDeploymentValidationScriptDescriptor>(
            _scripts
                .Where(script => script.AppliesToEnvironment(environmentName))
                .Where(script =>
                    script.Requirement == AzureDeploymentValidationScriptRequirement.Required ||
                    script.Requirement == AzureDeploymentValidationScriptRequirement.Blocking)
                .OrderBy(script => script.Key, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    public AzureDeploymentValidationScriptDescriptor? FindByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return _scripts.FirstOrDefault(script =>
            string.Equals(script.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
