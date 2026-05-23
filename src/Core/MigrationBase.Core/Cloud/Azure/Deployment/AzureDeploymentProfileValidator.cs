namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Performs SDK-free validation of Azure deployment profile descriptors.
/// </summary>
public sealed class AzureDeploymentProfileValidator
{
    public AzureDeploymentProfileValidationResult Validate(AzureDeploymentProfile? profile)
    {
        var result = AzureDeploymentProfileValidationResult.Success();

        if (profile is null)
        {
            result.Errors.Add("Deployment profile is required.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            result.Errors.Add("Deployment profile name is required.");
        }

        if (string.IsNullOrWhiteSpace(profile.EnvironmentName))
        {
            result.Errors.Add("Deployment profile environment name is required.");
        }

        if (string.IsNullOrWhiteSpace(profile.DefaultRegion))
        {
            result.Warnings.Add("Deployment profile default region is not set.");
        }

        if (profile.Targets.Count == 0)
        {
            result.Warnings.Add("Deployment profile does not define any deployment targets.");
            return result;
        }

        var duplicateTargetNames = profile.Targets
            .Where(t => !string.IsNullOrWhiteSpace(t.Name))
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var duplicateTargetName in duplicateTargetNames)
        {
            result.Errors.Add($"Duplicate deployment target name '{duplicateTargetName}'.");
        }

        foreach (var target in profile.Targets)
        {
            ValidateTarget(target, result);
        }

        return result;
    }

    private static void ValidateTarget(AzureDeploymentTargetDescriptor target, AzureDeploymentProfileValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(target.Name))
        {
            result.Errors.Add("Deployment target name is required.");
        }

        if (target.Kind == AzureDeploymentTargetKind.Unknown)
        {
            result.Errors.Add($"Deployment target '{target.Name}' must specify a target kind.");
        }

        if (string.IsNullOrWhiteSpace(target.HostRole))
        {
            result.Warnings.Add($"Deployment target '{target.Name}' does not declare a host role.");
        }

        if (string.IsNullOrWhiteSpace(target.ResourceGroupName))
        {
            result.Warnings.Add($"Deployment target '{target.Name}' does not declare a resource group.");
        }

        if (string.IsNullOrWhiteSpace(target.ResourceName))
        {
            result.Warnings.Add($"Deployment target '{target.Name}' does not declare a resource name.");
        }
    }
}
