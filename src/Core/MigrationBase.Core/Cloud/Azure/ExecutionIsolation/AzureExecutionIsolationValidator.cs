namespace MigrationBase.Core.Cloud.Azure.ExecutionIsolation;

public static class AzureExecutionIsolationValidator
{
    public static AzureExecutionIsolationValidationResult Validate(AzureExecutionIsolationProfile? profile)
    {
        var result = new AzureExecutionIsolationValidationResult();

        if (profile is null)
        {
            result.Errors.Add("Execution isolation profile is required.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(profile.ProfileName))
        {
            result.Errors.Add("Execution isolation profile name is required.");
        }

        if (string.IsNullOrWhiteSpace(profile.EnvironmentName))
        {
            result.Errors.Add("Execution isolation environment name is required.");
        }

        if (string.IsNullOrWhiteSpace(profile.DeploymentRing))
        {
            result.Errors.Add("Execution isolation deployment ring is required.");
        }

        if (profile.Boundaries.Count == 0)
        {
            result.Warnings.Add("Execution isolation profile has no explicit boundaries.");
        }

        foreach (var boundary in profile.Boundaries)
        {
            if (string.IsNullOrWhiteSpace(boundary.Name))
            {
                result.Errors.Add("Execution isolation boundary name is required.");
            }

            if (boundary.RequiresDedicatedSqlDatabase && boundary.AllowsSharedWorkers)
            {
                result.Warnings.Add($"Boundary '{boundary.Name}' requires a dedicated SQL database but allows shared workers.");
            }
        }

        return result;
    }
}
