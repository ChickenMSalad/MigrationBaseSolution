using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Telemetry;

public static class TelemetryEventWriterRegistrationExtensions
{
    public static IServiceCollection AddTelemetryEventWriter(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ITelemetryEventWriter, TelemetryEventWriter>();

        return services;
    }
}
