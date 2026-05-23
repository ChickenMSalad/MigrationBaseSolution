namespace MigrationBase.Core.Cloud.Azure.Workers;

public sealed record AzureWorkerLifecycleValidationResult
{
    public bool IsValid { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static AzureWorkerLifecycleValidationResult Valid() =>
        new() { IsValid = true };

    public static AzureWorkerLifecycleValidationResult Invalid(params string[] errors) =>
        new() { IsValid = false, Errors = errors };
}
