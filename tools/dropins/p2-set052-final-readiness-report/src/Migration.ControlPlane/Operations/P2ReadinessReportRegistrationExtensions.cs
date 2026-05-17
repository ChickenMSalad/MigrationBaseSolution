using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Operations;

public static class P2ReadinessReportRegistrationExtensions
{
    public static IServiceCollection AddP2ReadinessReport(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IP2ReadinessReportService, P2ReadinessReportService>();

        return services;
    }
}
