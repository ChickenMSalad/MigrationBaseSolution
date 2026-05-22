using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Audit;

public static class AuditEventWriterRegistrationExtensions
{
    public static IServiceCollection AddAuditEventWriter(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAuditEventWriter, AuditEventWriter>();

        return services;
    }
}
