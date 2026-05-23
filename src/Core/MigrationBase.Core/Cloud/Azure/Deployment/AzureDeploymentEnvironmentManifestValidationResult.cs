using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Deployment;

public sealed class AzureDeploymentEnvironmentManifestValidationResult
{
    private AzureDeploymentEnvironmentManifestValidationResult(IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
    {
        Errors = errors;
        Warnings = warnings;
    }

    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }
    public bool IsValid => Errors.Count == 0;

    public static AzureDeploymentEnvironmentManifestValidationResult Success(params string[] warnings)
        => new(Array.Empty<string>(), warnings ?? Array.Empty<string>());

    public static AzureDeploymentEnvironmentManifestValidationResult Failure(IEnumerable<string> errors, IEnumerable<string>? warnings = null)
        => new((errors ?? Array.Empty<string>()).Where(static x => !string.IsNullOrWhiteSpace(x)).ToArray(),
            (warnings ?? Array.Empty<string>()).Where(static x => !string.IsNullOrWhiteSpace(x)).ToArray());
}
