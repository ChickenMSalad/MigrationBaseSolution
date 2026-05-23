namespace MigrationBase.Core.Cloud.Azure.Execution;

public sealed class AzureExecutionEnvironmentProfileValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public static AzureExecutionEnvironmentProfileValidationResult Valid()
    {
        return new AzureExecutionEnvironmentProfileValidationResult();
    }

    public static AzureExecutionEnvironmentProfileValidationResult Invalid(params string[] errors)
    {
        var result = new AzureExecutionEnvironmentProfileValidationResult();
        foreach (var error in errors.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            result.Errors.Add(error);
        }

        return result;
    }
}
