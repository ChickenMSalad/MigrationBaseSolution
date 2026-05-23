namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// In-memory registry for Azure deployment parameters. Later P5 deployment automation can hydrate this from JSON, SQL, or IaC manifests.
/// </summary>
public sealed class AzureDeploymentParameterRegistry : IAzureDeploymentParameterRegistry
{
    private readonly IReadOnlyCollection<AzureDeploymentParameterDescriptor> _parameters;

    public AzureDeploymentParameterRegistry(IEnumerable<AzureDeploymentParameterDescriptor> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        _parameters = parameters
            .Where(static parameter => !string.IsNullOrWhiteSpace(parameter.Name))
            .GroupBy(static parameter => parameter.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .ToArray();
    }

    public IReadOnlyCollection<AzureDeploymentParameterDescriptor> GetAll() => _parameters;

    public AzureDeploymentParameterDescriptor? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public AzureDeploymentParameterValidationResult ValidateRequiredParameters(
        IReadOnlyDictionary<string, string?> suppliedParameters,
        string environmentName,
        string hostRole)
    {
        ArgumentNullException.ThrowIfNull(suppliedParameters);

        var errors = new List<string>();
        var warnings = new List<string>();
        var isProduction = string.Equals(environmentName, "prod", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "production", StringComparison.OrdinalIgnoreCase);

        foreach (var parameter in _parameters)
        {
            if (!Applies(parameter.AppliesToEnvironments, environmentName) || !Applies(parameter.AppliesToHostRoles, hostRole))
            {
                continue;
            }

            var required = parameter.Requirement == AzureDeploymentParameterRequirement.Required
                || (parameter.Requirement == AzureDeploymentParameterRequirement.RequiredForProduction && isProduction)
                || parameter.Requirement == AzureDeploymentParameterRequirement.SecretReference;

            if (!required)
            {
                continue;
            }

            if (!suppliedParameters.TryGetValue(parameter.Name, out var value) || string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"Missing required Azure deployment parameter '{parameter.Name}' for environment '{environmentName}' and host role '{hostRole}'.");
            }
            else if (parameter.IsSecret && !LooksLikeReference(value))
            {
                warnings.Add($"Azure deployment parameter '{parameter.Name}' is marked secret; prefer a Key Vault or managed identity reference instead of a literal value.");
            }
        }

        return new AzureDeploymentParameterValidationResult
        {
            Errors = errors,
            Warnings = warnings
        };
    }

    private static bool Applies(IReadOnlyCollection<string> values, string candidate)
    {
        return values.Count == 0
            || values.Contains("*", StringComparer.OrdinalIgnoreCase)
            || values.Contains(candidate, StringComparer.OrdinalIgnoreCase);
    }

    private static bool LooksLikeReference(string value)
    {
        return value.StartsWith("@Microsoft.KeyVault(", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("kv://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("secretref:", StringComparison.OrdinalIgnoreCase);
    }
}
