using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Artifacts;

/// <summary>
/// Captures the artifact set expected for one deployable release candidate.
/// </summary>
public sealed class AzureDeploymentArtifactManifest
{
    public AzureDeploymentArtifactManifest(
        string releaseKey,
        string buildId,
        string sourceCommit,
        DateTimeOffset createdAtUtc,
        IEnumerable<AzureDeploymentArtifactDescriptor> artifacts)
    {
        ReleaseKey = RequireText(releaseKey, nameof(releaseKey));
        BuildId = RequireText(buildId, nameof(buildId));
        SourceCommit = RequireText(sourceCommit, nameof(sourceCommit));
        CreatedAtUtc = createdAtUtc;
        Artifacts = artifacts?.ToArray() ?? throw new ArgumentNullException(nameof(artifacts));
    }

    public string ReleaseKey { get; }

    public string BuildId { get; }

    public string SourceCommit { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<AzureDeploymentArtifactDescriptor> Artifacts { get; }

    public IReadOnlyList<AzureDeploymentArtifactDescriptor> RequiredArtifacts =>
        Artifacts.Where(artifact => artifact.Required).ToArray();

    public AzureDeploymentArtifactManifestValidationResult Validate()
    {
        var issues = new List<string>();
        var duplicateKeys = Artifacts
            .GroupBy(artifact => artifact.ArtifactKey, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var duplicateKey in duplicateKeys)
        {
            issues.Add($"Duplicate artifact key '{duplicateKey}' was found.");
        }

        foreach (var artifact in Artifacts)
        {
            if (artifact.Kind == AzureDeploymentArtifactKind.Unknown)
            {
                issues.Add($"Artifact '{artifact.ArtifactKey}' has an unknown kind.");
            }
        }

        return new AzureDeploymentArtifactManifestValidationResult(issues.Count == 0, issues);
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
