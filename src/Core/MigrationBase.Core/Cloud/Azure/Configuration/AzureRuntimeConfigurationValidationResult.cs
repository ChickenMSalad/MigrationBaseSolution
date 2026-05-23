namespace MigrationBase.Core.Cloud.Azure.Configuration;

public sealed class AzureRuntimeConfigurationValidationResult
{
    public AzureRuntimeConfigurationValidationResult(IReadOnlyCollection<string> errors, IReadOnlyCollection<string> warnings)
    {
        Errors = errors;
        Warnings = warnings;
    }

    public IReadOnlyCollection<string> Errors { get; }

    public IReadOnlyCollection<string> Warnings { get; }

    public bool IsValid => Errors.Count == 0;

    public static AzureRuntimeConfigurationValidationResult Success { get; } = new(Array.Empty<string>(), Array.Empty<string>());
}
