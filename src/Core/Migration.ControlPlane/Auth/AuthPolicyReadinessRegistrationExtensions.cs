using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Auth;

public static class AuthPolicyReadinessRegistrationExtensions
{
    public static IServiceCollection AddAuthPolicyReadiness(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAuthPolicyReadinessService, AuthPolicyReadinessService>();

        return services;
    }
}
