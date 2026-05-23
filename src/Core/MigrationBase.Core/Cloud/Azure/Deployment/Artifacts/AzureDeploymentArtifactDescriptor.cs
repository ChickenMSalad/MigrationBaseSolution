using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Artifacts;

/// <summary>
/// Describes a deployable artifact produced by a build pipeline and consumed by an Azure deployment stage.
/// This contract is intentionally storage/provider neutral so it can describe zip packages, containers,
/// SQL scripts, configuration bundles, and validation evidence without taking an Azure SDK dependency.
/// </summary>
public sealed class AzureDeploymentArtifactDescriptor
{
    public AzureDeploymentArtifactDescriptor(
        string artifactKey,
        string displayName,
        AzureDeploymentArtifactKind kind,
        string producer,
        string relativePath,
        bool required,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArtifactKey = RequireText(artifactKey, nameof(artifactKey));
        DisplayName = RequireText(displayName, nameof(displayName));
        Kind = kind;
        Producer = RequireText(producer, nameof(producer));
        RelativePath = RequireText(relativePath, nameof(relativePath));
        Required = required;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public string ArtifactKey { get; }

    public string DisplayName { get; }

    public AzureDeploymentArtifactKind Kind { get; }

    public string Producer { get; }

    public string RelativePath { get; }

    public bool Required { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    public bool AppliesToRole(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            return false;
        }

        if (!Metadata.TryGetValue("roles", out var roles) || string.IsNullOrWhiteSpace(roles))
        {
            return true;
        }

        return roles
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(role => string.Equals(role, roleKey, StringComparison.OrdinalIgnoreCase));
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", name);
        }

        return value.Trim();
    }
}
