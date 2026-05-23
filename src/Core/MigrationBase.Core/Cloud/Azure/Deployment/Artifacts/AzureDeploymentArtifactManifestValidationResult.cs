using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Artifacts;

public sealed class AzureDeploymentArtifactManifestValidationResult
{
    public AzureDeploymentArtifactManifestValidationResult(bool isValid, IEnumerable<string> issues)
    {
        IsValid = isValid;
        Issues = issues?.ToArray() ?? System.Array.Empty<string>();
    }

    public bool IsValid { get; }

    public IReadOnlyList<string> Issues { get; }

    public static AzureDeploymentArtifactManifestValidationResult Success { get; } =
        new AzureDeploymentArtifactManifestValidationResult(true, System.Array.Empty<string>());
}
