using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Telemetry;

public static class TelemetryRegistrationExtensions
{
    public static IServiceCollection AddTelemetrySink(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Telemetry:Provider"] ?? "InMemory";

        if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ITelemetrySink, InMemoryTelemetrySink>();
            return services;
        }

        services.AddSingleton<ITelemetrySink, InMemoryTelemetrySink>();
        return services;
    }
}
