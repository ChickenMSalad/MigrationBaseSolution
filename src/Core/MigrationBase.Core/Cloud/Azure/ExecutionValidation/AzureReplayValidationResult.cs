namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public sealed record AzureReplayValidationResult
{
    public required string ValidationId { get; init; }

    public bool IsValid { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static AzureReplayValidationResult Success(string validationId) => new()
    {
        ValidationId = validationId,
        IsValid = true
    };

    public static AzureReplayValidationResult Failure(string validationId, IEnumerable<string> errors) => new()
    {
        ValidationId = validationId,
        IsValid = false,
        Errors = errors?.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray() ?? Array.Empty<string>()
    };
}
