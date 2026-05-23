namespace MigrationBase.Core.Cloud.Azure.Tenancy;

public sealed class AzureOperationalTenantBoundaryValidationResult
{
    private AzureOperationalTenantBoundaryValidationResult(bool isValid, IReadOnlyCollection<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public bool IsValid { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public static AzureOperationalTenantBoundaryValidationResult Passed()
    {
        return new AzureOperationalTenantBoundaryValidationResult(true, Array.Empty<string>());
    }

    public static AzureOperationalTenantBoundaryValidationResult Failed(params string[] errors)
    {
        return new AzureOperationalTenantBoundaryValidationResult(false, errors ?? Array.Empty<string>());
    }
}
