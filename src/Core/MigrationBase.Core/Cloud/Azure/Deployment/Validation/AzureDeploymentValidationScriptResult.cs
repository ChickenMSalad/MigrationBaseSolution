using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Validation;

/// <summary>
/// Captures the normalized result of a deployment validation script.
/// </summary>
public sealed class AzureDeploymentValidationScriptResult
{
    public AzureDeploymentValidationScriptResult(
        string scriptKey,
        bool succeeded,
        string summary,
        IEnumerable<string>? evidenceKeys = null,
        IEnumerable<string>? warnings = null,
        IEnumerable<string>? errors = null,
        DateTimeOffset? completedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(scriptKey))
        {
            throw new ArgumentException("Value is required.", nameof(scriptKey));
        }

        ScriptKey = scriptKey.Trim();
        Succeeded = succeeded;
        Summary = string.IsNullOrWhiteSpace(summary) ? string.Empty : summary.Trim();
        EvidenceKeys = ToReadOnlyList(evidenceKeys);
        Warnings = ToReadOnlyList(warnings);
        Errors = ToReadOnlyList(errors);
        CompletedAtUtc = completedAtUtc ?? DateTimeOffset.UtcNow;
    }

    public string ScriptKey { get; }

    public bool Succeeded { get; }

    public string Summary { get; }

    public IReadOnlyList<string> EvidenceKeys { get; }

    public IReadOnlyList<string> Warnings { get; }

    public IReadOnlyList<string> Errors { get; }

    public DateTimeOffset CompletedAtUtc { get; }

    public bool IsBlockingFailure(AzureDeploymentValidationScriptDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return !Succeeded &&
               (descriptor.Requirement == AzureDeploymentValidationScriptRequirement.Required ||
                descriptor.Requirement == AzureDeploymentValidationScriptRequirement.Blocking);
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
