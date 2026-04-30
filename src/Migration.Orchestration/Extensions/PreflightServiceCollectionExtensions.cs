using Microsoft.Extensions.DependencyInjection;
using Migration.Orchestration.Preflight;

namespace Migration.Orchestration.Extensions;

public static class PreflightServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationPreflight(this IServiceCollection services)
    {
        services.AddSingleton<IMigrationPreflightService, MigrationPreflightService>();
        return services;
    }
}
