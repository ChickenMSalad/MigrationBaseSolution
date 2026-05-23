namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Result returned after checking a deployment evidence manifest for required evidence gaps.
/// </summary>
public sealed record AzureDeploymentEvidenceValidationResult
{
    public bool IsReady { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> MissingRequiredEvidenceKeys { get; init; } = Array.Empty<string>();

    public static AzureDeploymentEvidenceValidationResult Ready() => new()
    {
        IsReady = true
    };

    public static AzureDeploymentEvidenceValidationResult NotReady(
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> missingRequiredEvidenceKeys) => new()
    {
        IsReady = false,
        Errors = errors,
        Warnings = warnings,
        MissingRequiredEvidenceKeys = missingRequiredEvidenceKeys
    };
}
