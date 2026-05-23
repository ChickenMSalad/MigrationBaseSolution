using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Validation;

/// <summary>
/// Describes a deployment validation script that can be used by deployment automation
/// to prove an Azure runtime environment is ready before promotion or execution.
/// </summary>
public sealed class AzureDeploymentValidationScriptDescriptor
{
    public AzureDeploymentValidationScriptDescriptor(
        string key,
        string displayName,
        string scriptPath,
        AzureDeploymentValidationScriptRequirement requirement,
        IEnumerable<string>? targetEnvironments = null,
        IEnumerable<string>? requiredSettings = null,
        IEnumerable<string>? requiredEvidenceKeys = null)
    {
        Key = NormalizeRequired(key, nameof(key));
        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        ScriptPath = NormalizeRequired(scriptPath, nameof(scriptPath));
        Requirement = requirement;
        TargetEnvironments = ToReadOnlyList(targetEnvironments);
        RequiredSettings = ToReadOnlyList(requiredSettings);
        RequiredEvidenceKeys = ToReadOnlyList(requiredEvidenceKeys);
    }

    public string Key { get; }

    public string DisplayName { get; }

    public string ScriptPath { get; }

    public AzureDeploymentValidationScriptRequirement Requirement { get; }

    public IReadOnlyList<string> TargetEnvironments { get; }

    public IReadOnlyList<string> RequiredSettings { get; }

    public IReadOnlyList<string> RequiredEvidenceKeys { get; }

    public bool AppliesToEnvironment(string environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return false;
        }

        return TargetEnvironments.Count == 0 ||
               TargetEnvironments.Any(candidate =>
                   string.Equals(candidate, environmentName, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static IReadOnlyList<string> ToReadOnlyList(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return Array.Empty<string>();
        }

        return new ReadOnlyCollection<string>(
            values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
    }
}
