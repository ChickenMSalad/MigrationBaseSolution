namespace MigrationBase.Core.Cloud.Azure.Workers.Retry;

public sealed record AzureWorkerRetryPolicyValidationResult
{
    private AzureWorkerRetryPolicyValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public bool IsValid { get; }

    public IReadOnlyList<string> Errors { get; }

    public static AzureWorkerRetryPolicyValidationResult Valid() => new(true, Array.Empty<string>());

    public static AzureWorkerRetryPolicyValidationResult Invalid(IEnumerable<string> errors) =>
        new(false, errors.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray());
}
