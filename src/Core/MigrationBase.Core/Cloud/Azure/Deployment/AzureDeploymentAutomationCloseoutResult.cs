namespace MigrationBase.Core.Cloud.Azure.Deployment;

public sealed class AzureDeploymentAutomationCloseoutResult
{
    public bool IsReadyForObservabilityHandoff => Errors.Count == 0;
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();

    public static AzureDeploymentAutomationCloseoutResult Ready()
    {
        return new AzureDeploymentAutomationCloseoutResult();
    }

    public static AzureDeploymentAutomationCloseoutResult Failed(params string[] errors)
    {
        var result = new AzureDeploymentAutomationCloseoutResult();
        foreach (var error in errors.Where(static value => !string.IsNullOrWhiteSpace(value)))
        {
            result.Errors.Add(error);
        }

        return result;
    }
}
