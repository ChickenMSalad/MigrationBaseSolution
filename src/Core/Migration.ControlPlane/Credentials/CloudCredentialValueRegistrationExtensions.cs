using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Credentials;

public static class CloudCredentialValueRegistrationExtensions
{
    public static IServiceCollection AddCloudCredentialValueProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var descriptor = CloudCredentialRegistrationExtensions.BuildDescriptor(configuration);
        var keyVaultUri = FirstNonEmpty(
            configuration["Cloud:KeyVaultUri"],
            configuration["KeyVault:Uri"],
            configuration["AzureKeyVault:Uri"]);

        if (descriptor.ProviderKind == CloudCredentialProviderKinds.KeyVault &&
            !string.IsNullOrWhiteSpace(keyVaultUri))
        {
            services.AddSingleton<ICloudCredentialValueProvider>(
                _ => new KeyVaultCloudCredentialValueProvider(new Uri(keyVaultUri)));

            return services;
        }

        services.AddSingleton<ICloudCredentialValueProvider, NullCloudCredentialValueProvider>();

        return services;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
