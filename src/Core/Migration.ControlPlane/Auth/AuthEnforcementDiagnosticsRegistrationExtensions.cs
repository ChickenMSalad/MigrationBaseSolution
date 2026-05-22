using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Auth;

public static class AuthEnforcementDiagnosticsRegistrationExtensions
{
    public static IServiceCollection AddAuthEnforcementDiagnostics(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAuthEnforcementDiagnosticsService, AuthEnforcementDiagnosticsService>();

        return services;
    }
}
