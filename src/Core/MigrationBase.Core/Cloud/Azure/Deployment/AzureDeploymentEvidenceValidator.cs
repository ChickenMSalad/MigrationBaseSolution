namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Performs SDK-free validation of deployment readiness evidence before an environment is considered executable.
/// </summary>
public static class AzureDeploymentEvidenceValidator
{
    public static AzureDeploymentEvidenceValidationResult Validate(
        AzureDeploymentEvidenceManifest manifest,
        IEnumerable<string> requiredEvidenceKeys)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(requiredEvidenceKeys);

        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(manifest.EnvironmentName))
        {
            errors.Add("EnvironmentName is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.TargetName))
        {
            errors.Add("TargetName is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.TargetKind))
        {
            errors.Add("TargetKind is required.");
        }

        var evidenceByKey = manifest.EvidenceItems
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        foreach (var duplicate in evidenceByKey.Where(pair => pair.Value.Length > 1).Select(pair => pair.Key))
        {
            warnings.Add($"Duplicate evidence key detected: {duplicate}.");
        }

        var missingRequired = requiredEvidenceKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Where(key => !evidenceByKey.TryGetValue(key, out var items) || !items.Any(item => item.IsSatisfied))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missingRequired.Length > 0)
        {
            errors.Add("One or more required deployment evidence items are missing or unsatisfied.");
        }

        return errors.Count == 0
            ? new AzureDeploymentEvidenceValidationResult
            {
                IsReady = true,
                Warnings = warnings
            }
            : AzureDeploymentEvidenceValidationResult.NotReady(errors, warnings, missingRequired);
    }
}
