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

        if (provider.Equals("ArtifactStorage", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("Artifact", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton(BuildArtifactOptions(configuration));
            services.AddSingleton<IAuditPersistenceProvider, ArtifactAuditPersistenceProvider>();
            return services;
        }

        services.AddSingleton<IAuditPersistenceProvider, InMemoryAuditPersistenceProvider>();
        return services;
    }

    private static ArtifactAuditPersistenceOptions BuildArtifactOptions(IConfiguration configuration)
    {
        return new ArtifactAuditPersistenceOptions
        {
            ArtifactKind = FirstNonEmpty(configuration["Audit:ArtifactKind"], "audit"),
            ArtifactId = FirstNonEmpty(configuration["Audit:ArtifactId"], "events"),
            FileNamePrefix = FirstNonEmpty(configuration["Audit:FileNamePrefix"], "audit-event"),
            RecentQueryLimit = ReadInt(configuration, "Audit:RecentQueryLimit", 100)
        };
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var parsed)
            ? fallback
            : Math.Clamp(parsed, 10, 1000);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
