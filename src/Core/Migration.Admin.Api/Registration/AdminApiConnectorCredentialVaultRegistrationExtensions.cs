using Microsoft.Extensions.DependencyInjection;

namespace Migration.Admin.Api.Registration;

public static class AdminApiConnectorCredentialVaultRegistrationExtensions
{
    public static IServiceCollection AddAdminApiConnectorCredentialVault(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}


