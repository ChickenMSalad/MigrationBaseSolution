namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Represents validation results for deployment parameter completeness and policy checks.
/// </summary>
public sealed record AzureDeploymentParameterValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static AzureDeploymentParameterValidationResult Valid { get; } = new();

    public static AzureDeploymentParameterValidationResult FromErrors(params string[] errors) => new()
    {
        Errors = errors.Where(static error => !string.IsNullOrWhiteSpace(error)).ToArray()
    };
}
