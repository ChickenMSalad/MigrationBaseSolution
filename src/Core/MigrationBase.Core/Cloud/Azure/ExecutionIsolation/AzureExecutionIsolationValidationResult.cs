namespace MigrationBase.Core.Cloud.Azure.ExecutionIsolation;

public sealed class AzureExecutionIsolationValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public static AzureExecutionIsolationValidationResult Valid()
    {
        return new AzureExecutionIsolationValidationResult();
    }

    public static AzureExecutionIsolationValidationResult Invalid(params string[] errors)
    {
        var result = new AzureExecutionIsolationValidationResult();
        result.Errors.AddRange(errors.Where(error => !string.IsNullOrWhiteSpace(error)));
        return result;
    }
}
