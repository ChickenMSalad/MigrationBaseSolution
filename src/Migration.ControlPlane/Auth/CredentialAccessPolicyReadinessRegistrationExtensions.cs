using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Auth;

public static class CredentialAccessPolicyReadinessRegistrationExtensions
{
    public static IServiceCollection AddCredentialAccessPolicyReadiness(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ICredentialAccessPolicyReadinessService, CredentialAccessPolicyReadinessService>();

        return services;
    }
}
