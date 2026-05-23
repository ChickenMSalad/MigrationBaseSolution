namespace MigrationBase.Core.Cloud.Azure.Infrastructure.Identity;

public sealed class AzureCredentialBoundaryValidator
{
    public AzureCredentialBoundaryValidationResult Validate(AzureCredentialBoundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);

        var result = new AzureCredentialBoundaryValidationResult();

        if (string.IsNullOrWhiteSpace(boundary.Name))
        {
            result.Errors.Add("Credential boundary name is required.");
        }

        if (boundary.ResolutionMode == AzureCredentialResolutionMode.Unspecified)
        {
            result.Errors.Add($"Credential boundary '{boundary.Name}' must specify a resolution mode.");
        }

        if (boundary.ResolutionMode == AzureCredentialResolutionMode.ManagedIdentity &&
            string.IsNullOrWhiteSpace(boundary.ManagedIdentityClientIdSettingName))
        {
            result.Warnings.Add($"Credential boundary '{boundary.Name}' uses managed identity without an explicit client id setting name.");
        }

        if (boundary.AllowsConnectionStringFallback)
        {
            result.Warnings.Add($"Credential boundary '{boundary.Name}' allows connection-string fallback; this should be disabled for production.");
        }

        return result;
    }
}
