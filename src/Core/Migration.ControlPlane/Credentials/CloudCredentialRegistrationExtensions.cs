using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Credentials;

public static class CloudCredentialRegistrationExtensions
{
    public static IServiceCollection AddCloudCredentialPlanning(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var prefix = FirstNonEmpty(
            configuration["Cloud:SecretNamePrefix"],
            configuration["CredentialProvider:SecretNamePrefix"],
            $"migration--workspace-{FirstNonEmpty(configuration["Workspace:WorkspaceId"], "default")}");

        services.AddSingleton<ICloudCredentialNameResolver>(_ => new CloudCredentialNameResolver(prefix));

        return services;
    }

    public static CloudCredentialProviderDescriptor BuildDescriptor(IConfiguration configuration)
    {
        var credentialMode = FirstNonEmpty(
            configuration["Cloud:CredentialMode"],
            configuration["CredentialProvider:Mode"],
            "userSecrets");

        var keyVaultUri = FirstNonEmpty(
            configuration["Cloud:KeyVaultUri"],
            configuration["KeyVault:Uri"],
            configuration["AzureKeyVault:Uri"]);

        var providerKind = credentialMode.Equals("keyVault", StringComparison.OrdinalIgnoreCase) ||
                           credentialMode.Equals("managedIdentity", StringComparison.OrdinalIgnoreCase) ||
                           !string.IsNullOrWhiteSpace(keyVaultUri)
            ? CloudCredentialProviderKinds.KeyVault
            : credentialMode.Equals("userSecrets", StringComparison.OrdinalIgnoreCase)
                ? CloudCredentialProviderKinds.UserSecrets
                : credentialMode.Equals("local", StringComparison.OrdinalIgnoreCase)
                    ? CloudCredentialProviderKinds.Local
                    : CloudCredentialProviderKinds.Unknown;

        var warnings = new List<string>();

        if (providerKind == CloudCredentialProviderKinds.KeyVault && string.IsNullOrWhiteSpace(keyVaultUri))
        {
            warnings.Add("Key Vault credential provider is selected but no Key Vault URI is configured.");
        }

        if (providerKind == CloudCredentialProviderKinds.Unknown)
        {
            warnings.Add($"Credential provider kind '{credentialMode}' is not recognized.");
        }

        var prefix = FirstNonEmpty(
            configuration["Cloud:SecretNamePrefix"],
            configuration["CredentialProvider:SecretNamePrefix"],
            $"migration--workspace-{FirstNonEmpty(configuration["Workspace:WorkspaceId"], "default")}");

        return new CloudCredentialProviderDescriptor(
            ProviderKind: providerKind,
            IsConfigured: providerKind != CloudCredentialProviderKinds.KeyVault || !string.IsNullOrWhiteSpace(keyVaultUri),
            UsesManagedIdentity: providerKind == CloudCredentialProviderKinds.KeyVault &&
                                 string.IsNullOrWhiteSpace(configuration["AzureKeyVault:ConnectionString"]),
            KeyVaultUriConfigured: string.IsNullOrWhiteSpace(keyVaultUri) ? null : "<configured>",
            SecretNamePrefix: prefix,
            SupportedSecretKinds: CloudCredentialSecretKinds.All,
            Warnings: warnings);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
