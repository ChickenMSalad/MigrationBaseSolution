using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Artifacts;

public sealed class AzureDeploymentArtifactManifestRegistry : IAzureDeploymentArtifactManifestRegistry
{
    private readonly IReadOnlyList<AzureDeploymentArtifactManifest> _manifests;

    public AzureDeploymentArtifactManifestRegistry(IEnumerable<AzureDeploymentArtifactManifest> manifests)
    {
        _manifests = manifests?.ToArray() ?? throw new ArgumentNullException(nameof(manifests));
    }

    public IReadOnlyList<AzureDeploymentArtifactManifest> GetManifests() => _manifests;

    public AzureDeploymentArtifactManifest? FindByReleaseKey(string releaseKey)
    {
        if (string.IsNullOrWhiteSpace(releaseKey))
        {
            return null;
        }

        return _manifests.FirstOrDefault(manifest =>
            string.Equals(manifest.ReleaseKey, releaseKey, StringComparison.OrdinalIgnoreCase));
    }
}
