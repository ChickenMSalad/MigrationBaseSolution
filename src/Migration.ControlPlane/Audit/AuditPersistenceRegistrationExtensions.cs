using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Audit;

public static class AuditPersistenceRegistrationExtensions
{
    public static IServiceCollection AddAuditPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Audit:Provider"] ?? "InMemory";

        if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAuditPersistenceProvider, InMemoryAuditPersistenceProvider>();
            return services;
        }

        services.AddSingleton<IAuditPersistenceProvider, InMemoryAuditPersistenceProvider>();
        return services;
    }
}
