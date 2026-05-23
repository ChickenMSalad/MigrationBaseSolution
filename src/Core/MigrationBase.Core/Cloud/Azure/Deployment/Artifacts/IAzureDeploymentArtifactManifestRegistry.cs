using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Artifacts;

public interface IAzureDeploymentArtifactManifestRegistry
{
    IReadOnlyList<AzureDeploymentArtifactManifest> GetManifests();

    AzureDeploymentArtifactManifest? FindByReleaseKey(string releaseKey);
}
