namespace Migration.Core.Azure.Identity;

/// <summary>
/// Lightweight validation helpers for identity and secret-boundary configuration.
/// These helpers intentionally return strings instead of throwing so hosts and validators can decide how strict to be.
/// </summary>
public static class AzureRuntimeIdentityValidation
{
    public static IReadOnlyList<string> Validate(AzureRuntimeIdentityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        if (options.Mode == AzureRuntimeIdentityMode.Unspecified)
        {
            errors.Add("Azure runtime identity mode is unspecified.");
        }

        if (options.RequiresUserAssignedClientId && string.IsNullOrWhiteSpace(options.UserAssignedClientId))
        {
            errors.Add("UserAssignedClientId is required when identity mode is UserAssignedManagedIdentity.");
        }

        if (options.Mode != AzureRuntimeIdentityMode.DeveloperCredential && options.AllowDeveloperCredential)
        {
            errors.Add("AllowDeveloperCredential should only be true when identity mode is DeveloperCredential.");
        }

        return errors;
    }

    public static IReadOnlyList<string> Validate(AzureRuntimeSecretBoundaryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        if (options.SqlConnectionSource == AzureRuntimeSecretSource.Unspecified)
        {
            errors.Add("SQL connection source is unspecified.");
        }

        if (options.ConnectorCredentialSource == AzureRuntimeSecretSource.Unspecified)
        {
            errors.Add("Connector credential source is unspecified.");
        }

        var usesKeyVault = options.SqlConnectionSource == AzureRuntimeSecretSource.KeyVault
            || options.StorageConnectionSource == AzureRuntimeSecretSource.KeyVault
            || options.QueueConnectionSource == AzureRuntimeSecretSource.KeyVault
            || options.ConnectorCredentialSource == AzureRuntimeSecretSource.KeyVault;

        if (usesKeyVault && string.IsNullOrWhiteSpace(options.KeyVaultUri))
        {
            errors.Add("KeyVaultUri is required when any secret source is KeyVault.");
        }

        return errors;
    }
}
