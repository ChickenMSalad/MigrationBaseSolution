namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Represents non-throwing validation results for Azure deployment profile contracts.
/// </summary>
public sealed class AzureDeploymentProfileValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; } = new();

    public List<string> Warnings { get; } = new();

    public static AzureDeploymentProfileValidationResult Success()
    {
        return new AzureDeploymentProfileValidationResult();
    }
}
