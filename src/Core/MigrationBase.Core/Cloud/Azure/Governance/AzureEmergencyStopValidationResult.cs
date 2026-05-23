namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed class AzureEmergencyStopValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public static AzureEmergencyStopValidationResult Success()
    {
        return new AzureEmergencyStopValidationResult();
    }

    public static AzureEmergencyStopValidationResult Failure(params string[] errors)
    {
        var result = new AzureEmergencyStopValidationResult();
        result.Errors.AddRange(errors.Where(error => !string.IsNullOrWhiteSpace(error)));
        return result;
    }
}
